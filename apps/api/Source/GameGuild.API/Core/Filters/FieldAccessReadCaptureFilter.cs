using System.Reflection;
using System.Text.Json.Nodes;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API;

/// <summary>
/// Field-level read capture (issue #186 criterion 1): for endpoints annotated with
/// <c>[DataMaskingResourceType]</c> — the surfaces that return personal and sensitive entity data —
/// records one <see cref="FieldAccessAudit"/> read row per classifier-detected sensitive field of
/// every entity in the response, using the trusted ambient actor context (actor, tenant, IP, user
/// agent) rather than anything the request supplies. Capture is fail-open: a capture failure logs a
/// warning and never alters or fails the response.
/// </summary>
/// <remarks>
///     The pass records the sensitive fields of returned entities (the compliance-relevant reads),
///     bounded by <see cref="FieldLevelAuditOptions.MaxReadCaptureItemsPerResponse"/> per request;
///     non-sensitive read tracking stays available through the recording service and API for any
///     other surface. Read observations persist no field values.
/// </remarks>
public sealed class FieldAccessReadCaptureFilter(
    FieldAccessAuditService fieldAccessAudit,
    IActorContextAccessor actors,
    IOptions<FieldLevelAuditOptions> options,
    ILogger<FieldAccessReadCaptureFilter> logger) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        try
        {
            if (options.Value.CaptureEndpointReads
                && GetEffectiveStatusCode(context) is < 400
                && context.ActionDescriptor.EndpointMetadata.OfType<DataMaskingResourceTypeAttribute>().LastOrDefault() is { } resourceType
                && TryGetValue(context.Result, out var value))
            {
                await CaptureReadsAsync(resourceType.ResourceType, value, context).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Fail-open: read capture must never change the response.
            logger.LogWarning(exception, "Field access read capture failed and was skipped.");
        }

        await next().ConfigureAwait(false);
    }

    private static int GetEffectiveStatusCode(ResultExecutingContext context) =>
        context.Result is IStatusCodeActionResult { StatusCode: { } statusCode }
            ? statusCode
            : context.HttpContext.Response.StatusCode;

    private async Task CaptureReadsAsync(string resourceType, object? value, ResultExecutingContext context)
    {
        var actor = actors.ActorContext;
        // Anonymous or tenant-less callers cannot be attributed; masking still applies via its own filter.
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } userId || actor.TenantId is not { } tenantId)
        {
            return;
        }

        var observations = new List<FieldReadObservation>();
        var itemBudget = Math.Max(1, options.Value.MaxReadCaptureItemsPerResponse);
        switch (value)
        {
            case JsonObject document:
                CollectFromJson(document, resourceType, observations, ref itemBudget);
                break;
            case JsonArray array:
                CollectFromJsonArray(array, resourceType, observations, ref itemBudget);
                break;
            default:
                CollectFromObject(value, resourceType, observations, ref itemBudget);
                break;
        }

        if (observations.Count == 0)
        {
            return;
        }

        var http = context.HttpContext;
        await fieldAccessAudit.RecordFieldReadsAsync(
            tenantId,
            userId,
            resourceType,
            observations,
            actor.TypedAttributes.IpAddress,
            actor.TypedAttributes.UserAgent,
            http.TraceIdentifier,
            http.Request.Path,
            http.RequestAborted).ConfigureAwait(false);
    }

    private static void CollectFromJson(
        JsonObject document, string resourceType, List<FieldReadObservation> observations, ref int itemBudget)
    {
        // A paged wrapper carries the entities in its items array; its own scalar properties are
        // transport metadata, not entity fields.
        if (document.TryGetPropertyValue("items", out var items) && items is JsonArray array)
        {
            CollectFromJsonArray(array, resourceType, observations, ref itemBudget);
            return;
        }

        if (itemBudget <= 0)
        {
            return;
        }

        if (document.TryGetPropertyValue("id", out var idNode) && TryReadJsonGuid(idNode, out var entityId))
        {
            itemBudget--;
            foreach (var (propertyName, node) in document)
            {
                if (propertyName.Equals("id", StringComparison.OrdinalIgnoreCase) || node is null)
                {
                    continue;
                }

                var text = node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var raw) ? raw : null;
                if (SensitiveFieldClassifier.Classify(resourceType, propertyName, text) is { IsSensitive: true })
                {
                    observations.Add(new FieldReadObservation(entityId, propertyName, text));
                }
            }
        }
    }

    private static void CollectFromJsonArray(
        JsonArray array, string resourceType, List<FieldReadObservation> observations, ref int itemBudget)
    {
        foreach (var item in array)
        {
            if (itemBudget <= 0)
            {
                return;
            }

            if (item is JsonObject document)
            {
                CollectFromJson(document, resourceType, observations, ref itemBudget);
            }
        }
    }

    private static void CollectFromObject(object? value, string resourceType, List<FieldReadObservation> observations, ref int itemBudget)
    {
        if (value is null)
        {
            return;
        }

        // Paged collections expose the entities through an Items property.
        var itemsProperty = value.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public);
        if (itemsProperty is not null && typeof(System.Collections.IEnumerable).IsAssignableFrom(itemsProperty.PropertyType)
            && itemsProperty.PropertyType != typeof(string))
        {
            var items = itemsProperty.GetValue(value) as System.Collections.IEnumerable;
            if (items is not null)
            {
                foreach (var item in items)
                {
                    if (itemBudget <= 0)
                    {
                        return;
                    }

                    CollectSingleItem(item, resourceType, observations, ref itemBudget);
                }
            }

            return;
        }

        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (itemBudget <= 0)
                {
                    return;
                }

                CollectSingleItem(item, resourceType, observations, ref itemBudget);
            }

            return;
        }

        CollectSingleItem(value, resourceType, observations, ref itemBudget);
    }

    private static void CollectSingleItem(object? item, string resourceType, List<FieldReadObservation> observations, ref int itemBudget)
    {
        if (item is null || itemBudget <= 0 || IsScalar(item.GetType()))
        {
            return;
        }

        var itemType = item.GetType();
        var idProperty = itemType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        if (idProperty is null || !TryReadClrGuid(idProperty.GetValue(item), out var entityId))
        {
            return;
        }

        itemBudget--;
        foreach (var property in itemType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod is null || !property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(item);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (propertyValue is not null && !IsScalar(property.PropertyType))
            {
                // Nested objects are not top-level entity fields.
                continue;
            }

            var text = propertyValue as string ?? propertyValue?.ToString();
            if (SensitiveFieldClassifier.Classify(resourceType, property.Name, text) is { IsSensitive: true })
            {
                observations.Add(new FieldReadObservation(entityId, property.Name, text));
            }
        }
    }

    private static bool TryReadJsonGuid(JsonNode? node, out Guid entityId)
    {
        switch (node)
        {
            case JsonValue value when value.TryGetValue<string>(out var text):
                return Guid.TryParse(text, out entityId);
            case JsonValue value when value.TryGetValue<Guid>(out var guid):
                entityId = guid;
                return true;
            default:
                entityId = Guid.Empty;
                return false;
        }
    }

    private static bool TryReadClrGuid(object? value, out Guid entityId)
    {
        switch (value)
        {
            case Guid guid:
                entityId = guid;
                return true;
            case string text when Guid.TryParse(text, out var parsed):
                entityId = parsed;
                return true;
            default:
                entityId = Guid.Empty;
                return false;
        }
    }

    private static bool TryGetValue(IActionResult result, out object? value)
    {
        if (result is ObjectResult { Value: not null and not ProblemDetails } objectResult)
        {
            value = objectResult.Value;
            return true;
        }

        if (result is JsonResult { Value: not null } jsonResult)
        {
            value = jsonResult.Value;
            return true;
        }

        value = null;
        return false;
    }

    private static bool IsScalar(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
        type == typeof(TimeSpan) || type == typeof(TimeOnly) || type == typeof(DateOnly);
}

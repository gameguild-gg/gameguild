using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace GameGuild.API;

/// <summary>Applies authorization data-masking rules before MVC serializes successful object responses.</summary>
public sealed class FieldMaskingResultFilter(
    IDataMaskingService maskingService,
    IOptions<JsonOptions> jsonOptions,
    ILogger<FieldMaskingResultFilter> logger) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (GetStatusCode(context.Result, context.HttpContext.Response.StatusCode) is >= 400 ||
            !TryGetValue(context.Result, out var value, out var declaredType, out var replace))
        {
            await next().ConfigureAwait(false);
            return;
        }

        if (value is null or ProblemDetails || IsScalar(value.GetType()))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var explicitResourceType = context.ActionDescriptor.EndpointMetadata
            .OfType<DataMaskingResourceTypeAttribute>()
            .LastOrDefault()
            ?.ResourceType;
        if (explicitResourceType is null)
        {
            await next().ConfigureAwait(false);
            return;
        }

        var resourceType = explicitResourceType;

        try
        {
            var maskedValue = await maskingService.ApplyAsync(
                    resourceType,
                    value,
                    declaredType ?? value.GetType(),
                    jsonOptions.Value.JsonSerializerOptions,
                    context.HttpContext.RequestAborted)
                .ConfigureAwait(false);
            replace(maskedValue);
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Could not evaluate data-masking policy for response resource {ResourceType}; returning a failure instead of unmasked data",
                resourceType);
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Response filtering unavailable",
                Detail = "The response could not be safely filtered.",
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ContentTypes = { "application/problem+json" }
            };
        }

        await next().ConfigureAwait(false);
    }

    private static bool TryGetValue(
        IActionResult result,
        out object? value,
        out Type? declaredType,
        out Action<object?> replace)
    {
        switch (result)
        {
            case ObjectResult objectResult:
                value = objectResult.Value;
                declaredType = objectResult.DeclaredType;
                replace = updated =>
                {
                    objectResult.Value = updated;
                    if (updated is JsonNode)
                    {
                        objectResult.DeclaredType = typeof(JsonNode);
                    }
                };
                return true;
            case JsonResult jsonResult:
                value = jsonResult.Value;
                declaredType = null;
                replace = updated => jsonResult.Value = updated;
                return true;
            default:
                value = null;
                declaredType = null;
                replace = _ => { };
                return false;
        }
    }

    private static int? GetStatusCode(IActionResult result, int currentStatusCode) =>
        result is IStatusCodeActionResult { StatusCode: { } statusCode } ? statusCode : currentStatusCode;

    private static bool IsScalar(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
        type == typeof(TimeSpan);
}

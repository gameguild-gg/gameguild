using System.Text.Json;
using System.Text.Json.Nodes;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>Loads applicable rules and masks matching fields in serialized response data.</summary>
public sealed class DataMaskingService(
    IDataMaskingRuleRepository ruleRepository,
    IActorContextAccessor actorContextAccessor) : IDataMaskingService
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public Task<object?> ApplyAsync(
        string resourceType,
        object value,
        JsonSerializerOptions serializerOptions)
        => ApplyAsync(resourceType, value, value.GetType(), serializerOptions, CancellationToken.None);

    public Task<object?> ApplyAsync(
        string resourceType,
        object value,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken)
        => ApplyAsync(resourceType, value, value.GetType(), serializerOptions, cancellationToken);

    public async Task<object?> ApplyAsync(
        string resourceType,
        object value,
        Type serializationType,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(serializationType);
        ArgumentNullException.ThrowIfNull(serializerOptions);

        var globalRules = await ruleRepository.GetByResourceTypeAsync(resourceType, null, cancellationToken)
            .ConfigureAwait(false);
        var tenantRules = Actor.TenantId.HasValue
            ? await ruleRepository.GetByResourceTypeAsync(resourceType, Actor.TenantId, cancellationToken).ConfigureAwait(false)
            : new List<DataMaskingRule>();

        var applicableRules = tenantRules
            .Where(rule => rule.IsEnabled && !IsExempt(rule))
            .Select(rule => (Rule: rule, IsTenantRule: true))
            .Concat(globalRules.Where(rule => rule.IsEnabled && !IsExempt(rule))
                .Select(rule => (Rule: rule, IsTenantRule: false)))
            .GroupBy(entry => NormalizeFieldName(entry.Rule.FieldName), StringComparer.Ordinal)
            .Where(group => !string.IsNullOrEmpty(group.Key))
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(entry => entry.IsTenantRule)
                    .ThenByDescending(entry => entry.Rule.Priority)
                    .ThenBy(entry => entry.Rule.Id)
                    .First().Rule,
                StringComparer.Ordinal);

        if (applicableRules.Count == 0)
        {
            return value;
        }

        var root = JsonSerializer.SerializeToNode(value, serializationType, serializerOptions);
        if (root is null)
        {
            return value;
        }

        ApplyRules(root, string.Empty, applicableRules, serializerOptions);
        return root;
    }

    private bool IsExempt(DataMaskingRule rule)
    {
        if (Actor.SubjectIdAsGuid is { } userId && rule.IsUserExempt(userId))
        {
            return true;
        }

        var exemptRoles = DeserializeStringList(rule.ExemptRoles);
        if (exemptRoles.Any(exemptRole => Actor.Roles.Any(actorRole =>
                string.Equals(actorRole, exemptRole, StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        var requiredPermissions = DeserializeStringList(rule.RequiredPermissions);
        return requiredPermissions.Count > 0 && Actor.HasAllPermissions(requiredPermissions.ToArray());
    }

    private static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)?.Where(value => !string.IsNullOrWhiteSpace(value)).ToList()
                   ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static void ApplyRules(
        JsonNode node,
        string parentPath,
        IReadOnlyDictionary<string, DataMaskingRule> rules,
        JsonSerializerOptions serializerOptions)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                var fullPath = string.IsNullOrEmpty(parentPath) ? property.Key : $"{parentPath}.{property.Key}";
                var fieldRule = FindRule(rules, property.Key, fullPath);
                if (fieldRule is not null && property.Value is not null)
                {
                    var rawValue = ReadScalar(property.Value, serializerOptions) ?? property.Value.ToJsonString(serializerOptions);
                    jsonObject[property.Key] = JsonValue.Create(fieldRule.ApplyMasking(rawValue));
                }
                else if (property.Value is not null)
                {
                    ApplyRules(property.Value, fullPath, rules, serializerOptions);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                if (child is not null)
                {
                    ApplyRules(child, parentPath, rules, serializerOptions);
                }
            }
        }
    }

    private static DataMaskingRule? FindRule(
        IReadOnlyDictionary<string, DataMaskingRule> rules,
        string fieldName,
        string fullPath)
    {
        var normalizedPath = NormalizeFieldName(fullPath);
        var normalizedFieldName = NormalizeFieldName(fieldName);
        if (!string.Equals(normalizedPath, normalizedFieldName, StringComparison.Ordinal) &&
            rules.TryGetValue(normalizedPath, out var pathRule))
        {
            return pathRule;
        }

        return rules.TryGetValue(normalizedFieldName, out var fieldRule) ? fieldRule : null;
    }

    private static string? ReadScalar(JsonNode node, JsonSerializerOptions serializerOptions)
    {
        if (node is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<string>(out var stringValue))
        {
            return stringValue;
        }

        return value.ToJsonString(serializerOptions);
    }

    private static string NormalizeFieldName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join('.', value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => string.Concat(segment.Where(char.IsLetterOrDigit)).ToLowerInvariant()));
    }
}

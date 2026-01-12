using System.Text.Json;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Database-backed implementation of policy definition store.
///     Uses the IPolicyDefinitionRepository for persistence.
/// </summary>
public sealed class DatabasePolicyDefinitionStore(IPolicyDefinitionRepository repository) : IPolicyDefinitionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<PolicyDefinition?> GetPolicyAsync(
        string policyName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        Guid? tenantGuid = string.IsNullOrEmpty(tenantId) ? null : Guid.TryParse(tenantId, out var g) ? g : null;
        
        var entity = await repository.GetByNameAsync(policyName, tenantGuid, cancellationToken).ConfigureAwait(false);
        
        return entity == null ? null : MapToDefinition(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDefinition>> GetTenantPoliciesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return [];

        var entities = await repository.GetByTenantAsync(tenantGuid, includeGlobal: true, cancellationToken).ConfigureAwait(false);
        
        return entities.Select(MapToDefinition).ToList();
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return 0;

        var policies = await repository.GetByTenantAsync(tenantGuid, includeGlobal: false, cancellationToken).ConfigureAwait(false);
        
        // Return the max version across all tenant policies
        return policies.Count > 0 ? policies.Max(p => p.PolicyVersion) : 0;
    }

    /// <summary>
    ///     Maps a PolicyDefinitionEntity to a PolicyDefinition value object.
    /// </summary>
    private static PolicyDefinition MapToDefinition(PolicyDefinitionEntity entity)
    {
        return new PolicyDefinition
        {
            PolicyName = entity.PolicyName,
            RequireAuthentication = entity.RequireAuthentication,
            AuthenticationSchemes = DeserializeList(entity.AuthenticationSchemesJson),
            RequiredPermissions = DeserializeList(entity.RequiredPermissionsJson),
            RequiredRoles = DeserializeList(entity.RequiredRolesJson),
            RequireAccessControlListAccess = entity.RequireAccessControlListAccess,
            ResourceType = entity.ResourceType,
            MinimumAccessLevel = entity.MinimumAccessLevel,
            IsTenantScoped = entity.IsTenantScoped,
            Version = entity.PolicyVersion,
            UseRuleBasedEvaluation = entity.UseRuleBasedEvaluation,
            Rules = DeserializeRules(entity.RulesJson)
        };
    }

    private static IReadOnlyList<string> DeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<PolicyRule>? DeserializeRules(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            var rules = JsonSerializer.Deserialize<List<RuleDto>>(json, JsonOptions);
            return rules?.Select(r => new PolicyRule
            {
                Type = r.Type ?? string.Empty,
                Description = r.Description,
                Params = r.Params?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value),
                Enabled = r.Enabled
            }).ToList();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     DTO for deserializing rules from JSON.
    /// </summary>
    // ReSharper disable UnusedAutoPropertyAccessor.Local - Properties set by JSON deserializer
    // ReSharper disable CollectionNeverUpdated.Local - Collection populated by JSON deserializer
    private sealed class RuleDto
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, JsonElement>? Params { get; set; }
        public bool Enabled { get; set; } = true;
    }
    // ReSharper restore CollectionNeverUpdated.Local
    // ReSharper restore UnusedAutoPropertyAccessor.Local
}

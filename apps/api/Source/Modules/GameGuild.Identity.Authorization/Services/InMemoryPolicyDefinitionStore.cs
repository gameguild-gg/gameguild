using System.Collections.Concurrent;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     In-memory implementation of policy definition store for development/testing.
///     WARNING: This is NOT suitable for production - data is lost on restart.
///     For production, use DatabasePolicyDefinitionStore with CachedPolicyDefinitionStore wrapper.
/// </summary>
public sealed class InMemoryPolicyDefinitionStore : IPolicyDefinitionStore
{
    private readonly ConcurrentDictionary<string, PolicyDefinition> _basePolicies = new();
    private readonly ConcurrentDictionary<(string TenantId, string PolicyName), PolicyDefinition> _tenantPolicies = new();
    private readonly ConcurrentDictionary<string, long> _versions = new();

    /// <summary>
    ///     Initializes a new instance of <see cref="InMemoryPolicyDefinitionStore"/> with default policies.
    /// </summary>
    public InMemoryPolicyDefinitionStore()
    {
        // Register some default policies
        RegisterDefaultPolicies();
    }

    /// <inheritdoc />
    public Task<PolicyDefinition?> GetPolicyAsync(
        string policyName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        // Try tenant-specific first
        if (!string.IsNullOrEmpty(tenantId) &&
            _tenantPolicies.TryGetValue((tenantId, policyName), out var tenantPolicy))
        {
            return Task.FromResult<PolicyDefinition?>(tenantPolicy);
        }

        // Fall back to base policy
        _basePolicies.TryGetValue(policyName, out var basePolicy);
        return Task.FromResult(basePolicy);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PolicyDefinition>> GetTenantPoliciesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var policies = _tenantPolicies
            .Where(kvp => kvp.Key.TenantId == tenantId)
            .Select(kvp => kvp.Value)
            .ToList();

        return Task.FromResult<IReadOnlyList<PolicyDefinition>>(policies);
    }

    /// <inheritdoc />
    public Task<long> GetVersionAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var version = _versions.GetOrAdd(tenantId, 1L);
        return Task.FromResult(version);
    }

    /// <summary>
    ///     Registers a base (non-tenant-specific) policy.
    /// </summary>
    /// <param name="policy">The policy definition to register.</param>
    public void RegisterBasePolicy(PolicyDefinition policy)
    {
        _basePolicies[policy.PolicyName] = policy;
    }

    /// <summary>
    ///     Registers a tenant-specific policy override.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="policy">The policy definition to register.</param>
    public void RegisterTenantPolicy(string tenantId, PolicyDefinition policy)
    {
        _tenantPolicies[(tenantId, policy.PolicyName)] = policy;
        _versions.AddOrUpdate(tenantId, 2L, (_, v) => v + 1);
    }

    private void RegisterDefaultPolicies()
    {
        // Authenticated user policy
        RegisterBasePolicy(new PolicyDefinition
        {
            PolicyName = "Authenticated",
            RequireAuthentication = true,
            Version = 1,
            UseRuleBasedEvaluation = true,
            Rules = []
        });

        // Tenant member policy
        RegisterBasePolicy(new PolicyDefinition
        {
            PolicyName = "TenantMember",
            RequireAuthentication = true,
            Version = 1,
            UseRuleBasedEvaluation = true,
            Rules =
            [
                new PolicyRule
                {
                    Type = "TenantMatch",
                    Description = "Require tenant match",
                    Enabled = true
                }
            ]
        });

        // Admin policy
        RegisterBasePolicy(new PolicyDefinition
        {
            PolicyName = "Admin",
            RequireAuthentication = true,
            Version = 1,
            UseRuleBasedEvaluation = true,
            Rules =
            [
                new PolicyRule
                {
                    Type = "TenantMatch",
                    Description = "Require tenant match",
                    Enabled = true
                },
                new PolicyRule
                {
                    Type = "RequireAllPermissions",
                    Description = "Require admin permission",
                    Params = new Dictionary<string, object> { ["permissions"] = new[] { "admin" } },
                    Enabled = true
                }
            ]
        });
    }
}

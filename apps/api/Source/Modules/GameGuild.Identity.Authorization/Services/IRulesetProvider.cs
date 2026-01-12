
namespace GameGuild.Identity.Authorization;

/// <summary>
///     Provides rulesets for policies from the database.
/// </summary>
public interface IRulesetProvider
{
    /// <summary>
    ///     Gets the ruleset for a policy by name.
    /// </summary>
    /// <param name="policyName">The policy name (e.g., "Users.Edit")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy ruleset or null if not found</returns>
    Task<PolicyRuleset?> GetRulesetAsync(string policyName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the ruleset for a policy, considering tenant-specific overrides.
    /// </summary>
    /// <param name="policyName">The policy name (e.g., "Users.Edit")</param>
    /// <param name="tenantId">The tenant ID for tenant-specific overrides</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The policy ruleset or null if not found</returns>
    Task<PolicyRuleset?> GetRulesetForTenantAsync(
        string policyName,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates the cache for a specific policy.
    /// </summary>
    /// <param name="policyName">The policy name to invalidate</param>
    void InvalidatePolicy(string policyName);

    /// <summary>
    ///     Invalidates all cached policies.
    /// </summary>
    void InvalidateAll();
}

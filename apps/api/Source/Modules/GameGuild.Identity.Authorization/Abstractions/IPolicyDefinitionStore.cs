namespace GameGuild.Identity.Authorization;

/// <summary>
///     Stores and retrieves policy definitions from a persistent store (database).
/// </summary>
public interface IPolicyDefinitionStore
{
    /// <summary>
    ///     Gets a policy definition by name, optionally scoped to a tenant.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">Optional tenant ID for tenant-scoped policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy definition, or null if not found.</returns>
    Task<PolicyDefinition?> GetPolicyAsync(
        string policyName,
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all policies for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All policy definitions for the tenant.</returns>
    Task<IReadOnlyList<PolicyDefinition>> GetTenantPoliciesAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current version for a tenant's policies.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version number for cache invalidation.</returns>
    Task<long> GetVersionAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}

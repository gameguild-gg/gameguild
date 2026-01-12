namespace GameGuild.Identity.Authorization;

/// <summary>
///     Repository interface for managing policy definition entities.
/// </summary>
public interface IPolicyDefinitionRepository
{
    /// <summary>
    ///     Gets a policy by its ID.
    /// </summary>
    /// <param name="id">The policy ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy entity, or null if not found.</returns>
    Task<PolicyDefinitionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a policy by name, optionally scoped to a tenant.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">Optional tenant ID for tenant-scoped policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The policy entity, or null if not found.</returns>
    Task<PolicyDefinitionEntity?> GetByNameAsync(string policyName, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all policies for a tenant (including global policies).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="includeGlobal">Whether to include global (non-tenant-scoped) policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All policy entities for the tenant.</returns>
    Task<IReadOnlyList<PolicyDefinitionEntity>> GetByTenantAsync(Guid tenantId, bool includeGlobal = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all global (non-tenant-scoped) policies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All global policy entities.</returns>
    Task<IReadOnlyList<PolicyDefinitionEntity>> GetGlobalPoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active policies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All active policy entities.</returns>
    Task<IReadOnlyList<PolicyDefinitionEntity>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new policy.
    /// </summary>
    /// <param name="policy">The policy to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(PolicyDefinitionEntity policy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing policy.
    /// </summary>
    /// <param name="policy">The policy to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(PolicyDefinitionEntity policy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a policy (soft delete).
    /// </summary>
    /// <param name="policy">The policy to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(PolicyDefinitionEntity policy, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

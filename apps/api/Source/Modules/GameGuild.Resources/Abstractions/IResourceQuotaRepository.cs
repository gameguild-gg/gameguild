
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for managing resource quotas
/// </summary>
public interface IResourceQuotaRepository
{
    Task<ResourceQuota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ResourceQuota?> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<ResourceQuota> CreateAsync(ResourceQuota quota, CancellationToken cancellationToken = default);

    Task<ResourceQuota> UpdateAsync(ResourceQuota quota, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetActiveQuotasAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetQuotasExceedingLimitsAsync(ResourceUsageType? type = null, bool softLimitOnly = false, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetQuotasDueForResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Atomically attempts to increment quota usage with optimistic concurrency.
    ///     Validates against hard limit before incrementing.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="type">Resource usage type</param>
    /// <param name="amount">Amount to increment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     Tuple containing: Success (true if increment succeeded), Quota (current quota state).
    ///     Returns (true, null) if no quota exists (unlimited).
    ///     Returns (false, quota) if would exceed hard limit.
    /// </returns>
    Task<(bool Success, ResourceQuota? Quota)> TryIncrementUsageAsync(Guid tenantId, ResourceUsageType type, long amount, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Decrements quota usage. Ensures usage never goes negative.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="type">Resource usage type</param>
    /// <param name="amount">Amount to decrement</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if decrement was applied, false if quota not found</returns>
    Task<bool> DecrementUsageAsync(Guid tenantId, ResourceUsageType type, long amount, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets multiple quotas by tenant and types in a single query (batch operation).
    ///     Used to avoid N+1 query patterns when checking multiple resource types.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="types">Collection of resource usage types to fetch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping resource types to their quotas (missing types = no quota configured)</returns>
    Task<Dictionary<ResourceUsageType, ResourceQuota>> GetByTenantAndTypesAsync(
        Guid tenantId,
        IEnumerable<ResourceUsageType> types,
        CancellationToken cancellationToken = default);

    // User-level quota methods
    Task<ResourceQuota?> GetByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default);
}

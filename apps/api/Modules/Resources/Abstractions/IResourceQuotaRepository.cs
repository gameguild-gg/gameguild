namespace GameGuild.Modules.Resources;

/// <summary>
///     Repository interface for resource quota data access operations
/// </summary>
public interface IResourceQuotaRepository
{
    // Quota Management
    /// <summary>
    ///     Get resource quota by tenant ID and usage type
    /// </summary>
    Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all quotas for a tenant
    /// </summary>
    Task<IReadOnlyList<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new resource quota
    /// </summary>
    Task<ResourceQuota> CreateQuotaAsync(ResourceQuota quota, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing resource quota
    /// </summary>
    Task<ResourceQuota> UpdateQuotaAsync(ResourceQuota quota, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a resource quota
    /// </summary>
    Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    // Usage Record Management
    /// <summary>
    ///     Get usage record for a specific date and tenant/type
    /// </summary>
    Task<ResourceUsageRecord?> GetUsageRecordAsync(Guid tenantId, ResourceUsageType type, DateTime periodStart, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new usage record
    /// </summary>
    Task<ResourceUsageRecord> CreateUsageRecordAsync(ResourceUsageRecord usageRecord, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing usage record
    /// </summary>
    Task<ResourceUsageRecord> UpdateUsageRecordAsync(ResourceUsageRecord usageRecord, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get usage history for a resource type
    /// </summary>
    Task<IReadOnlyList<ResourceUsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenants exceeding resource limits
    /// </summary>
    Task<IReadOnlyList<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all active quotas that have last reset date
    /// </summary>
    Task<IReadOnlyList<ResourceQuota>> GetActiveQuotasWithLastResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get old usage records before a specific date
    /// </summary>
    Task<IReadOnlyList<ResourceUsageRecord>> GetOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Remove multiple usage records
    /// </summary>
    Task RemoveUsageRecordsAsync(IEnumerable<ResourceUsageRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get sum of usage count for a specific tenant, type and period
    /// </summary>
    Task<long> GetTotalUsageAsync(Guid tenantId, ResourceUsageType type, DateTime periodStart, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Save all pending changes
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

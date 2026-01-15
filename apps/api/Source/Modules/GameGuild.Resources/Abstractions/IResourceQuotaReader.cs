namespace GameGuild.Resources;

/// <summary>
///     Read-only operations for resource quotas.
///     Use this interface when you only need to query quota information.
/// </summary>
/// <remarks>
///     Part of the ISP-compliant split of IResourceQuotaService.
///     Consumers that only need to display quota info should depend on this interface.
/// </remarks>
public interface IResourceQuotaReader
{
    /// <summary>
    ///     Get resource quota for a tenant and usage type
    /// </summary>
    Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all quotas for a tenant
    /// </summary>
    Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get current usage for a resource type
    /// </summary>
    Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get usage history for a resource type
    /// </summary>
    Task<IEnumerable<UsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

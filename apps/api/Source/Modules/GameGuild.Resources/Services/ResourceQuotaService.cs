namespace GameGuild.Resources;

/// <summary>
///     Thin facade that delegates to focused sub-services for backward compatibility.
///     <para>
///     New code should depend on the specific sub-service interface needed:
///     <see cref="IQuotaManagementService"/>, <see cref="IQuotaEnforcementService"/>,
///     or <see cref="IQuotaMaintenanceService"/>.
///     </para>
/// </summary>
public class ResourceQuotaService(
    IQuotaManagementService management,
    IQuotaEnforcementService enforcement,
    IQuotaMaintenanceService maintenance) : IResourceQuotaService
{
    // --- IResourceQuotaReader (delegated to management) ---

    public Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
        => management.GetQuotaAsync(tenantId, type, cancellationToken);

    public Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => management.GetTenantQuotasAsync(tenantId, cancellationToken);

    public Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
        => management.GetCurrentUsageAsync(tenantId, type, cancellationToken);

    public Task<IEnumerable<UsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        => management.GetUsageHistoryAsync(tenantId, type, fromDate, toDate, cancellationToken);

    // --- IResourceQuotaWriter (delegated to management) ---

    public Task<ResourceQuota> SetQuotaAsync(Guid tenantId, ResourceUsageType type, long? softLimit, long? hardLimit, ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly, CancellationToken cancellationToken = default)
        => management.SetQuotaAsync(tenantId, type, softLimit, hardLimit, period, cancellationToken);

    public Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
        => management.DeleteQuotaAsync(tenantId, type, cancellationToken);

    // --- IResourceQuotaEnforcer (delegated to enforcement) ---

    public Task<ResourceLimitCheckResponse> CheckLimitsAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default)
        => enforcement.CheckLimitsAsync(tenantId, type, requestedAmount, cancellationToken);

    public Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(Guid tenantId, Dictionary<ResourceUsageType, long> requestedAmounts, CancellationToken cancellationToken = default)
        => enforcement.CheckMultipleLimitsAsync(tenantId, requestedAmounts, cancellationToken);

    public Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default)
        => enforcement.TryConsumeResourceAsync(tenantId, type, amount, userId, source, cancellationToken);

    public Task<(bool Success, long CurrentUsage, long? HardLimit)> TryAtomicConsumeAsync(Guid tenantId, ResourceUsageType type, long amount = 1, CancellationToken cancellationToken = default)
        => enforcement.TryAtomicConsumeAsync(tenantId, type, amount, cancellationToken);

    public Task<bool> DecrementUsageAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default)
        => enforcement.DecrementUsageAsync(tenantId, type, amount, userId, source, cancellationToken);

    // --- IResourceQuotaAnalytics (delegated to maintenance) ---

    public Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default)
        => maintenance.GetResourceUsageDetailsAsync(tenantId, type, historyDays, cancellationToken);

    public Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default)
        => maintenance.GetTenantsExceedingLimitsAsync(type, hardLimitOnly, cancellationToken);

    // --- IResourceQuotaMaintenance (delegated to maintenance) ---

    public Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default)
        => maintenance.ResetExpiredQuotasAsync(cancellationToken);

    public Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        => maintenance.CleanupOldUsageRecordsAsync(olderThan, cancellationToken);

    public Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
        => maintenance.RecalculateUsageAsync(tenantId, type, cancellationToken);
}

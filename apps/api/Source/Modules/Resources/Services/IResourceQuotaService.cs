using GameGuild.Modules.Resources.Models;


namespace GameGuild.Modules.Resources.Services;

/// <summary> Service for managing resource quotas and usage tracking </summary>
public interface IResourceQuotaService {
  // Quota Management
  /// <summary> Create or update a resource quota for a tenant </summary>
  Task<ResourceQuota> SetQuotaAsync(Guid tenantId, ResourceUsageType type, long? softLimit, long? hardLimit, ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly, CancellationToken cancellationToken = default);

  /// <summary> Get resource quota for a tenant and usage type </summary>
  Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

  /// <summary> Get all quotas for a tenant </summary>
  Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default);

  /// <summary> Delete a resource quota </summary>
  Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

  // Usage Tracking
  /// <summary> Record resource usage </summary>
  Task<bool> RecordUsageAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

  /// <summary> Get current usage for a resource type </summary>
  Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

  /// <summary> Get usage history for a resource type </summary>
  Task<IEnumerable<ResourceUsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

  // Limit Checking
  /// <summary> Check if a resource usage would exceed limits </summary>
  Task<ResourceLimitCheckResponse> CheckLimitsAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default);

  /// <summary> Check limits for multiple resource types </summary>
  Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(Guid tenantId, Dictionary<ResourceUsageType, long> requestedAmounts, CancellationToken cancellationToken = default);

  /// <summary> Attempt to consume resources if within limits </summary>
  Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default);

  // Analytics and Reporting
  /// <summary> Get comprehensive usage information for a tenant </summary>
  Task<MultiResourceUsageResponse> GetTenantUsageOverviewAsync(Guid tenantId, CancellationToken cancellationToken = default);

  /// <summary> Get detailed usage information for a specific resource type </summary>
  Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default);

  /// <summary> Get tenants that have exceeded their limits </summary>
  Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default);

  // Maintenance
  /// <summary> Reset quotas that are due for reset based on their period </summary>
  Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default);

  /// <summary> Clean up old usage records </summary>
  Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

  /// <summary> Recalculate current usage from usage records </summary>
  Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);
}

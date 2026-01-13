
namespace GameGuild.Resources;

/// <summary>
///     Service for managing resource quotas and usage tracking
/// </summary>
public interface IResourceQuotaService
{
    // Quota Management
    /// <summary>
    ///     Create or update a resource quota for a tenant
    /// </summary>
    Task<ResourceQuota> SetQuotaAsync(Guid tenantId, ResourceUsageType type, long? softLimit, long? hardLimit, ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get resource quota for a tenant and usage type
    /// </summary>
    Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all quotas for a tenant
    /// </summary>
    Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a resource quota
    /// </summary>
    Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    // Usage Tracking
    /// <summary>
    ///     Record resource usage. This method now enforces hard limits.
    /// </summary>
    /// <remarks>
    ///     DEPRECATED: Prefer using TryAtomicConsumeAsync for atomic operations with concurrency safety.
    ///     This method enforces hard limits but is NOT atomic under concurrent access.
    /// </remarks>
    [Obsolete("Use TryAtomicConsumeAsync for atomic, concurrency-safe quota consumption. This method is not atomic.")]
    Task<bool> RecordUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        Guid? userId = null,
        string? source = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Get current usage for a resource type
    /// </summary>
    Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get usage history for a resource type
    /// </summary>
    Task<IEnumerable<UsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    // Limit Checking
    /// <summary>
    ///     Check if a resource usage would exceed limits.
    ///     <para>
    ///     <b>ADVISORY ONLY:</b> This method is read-only and does not consume quota.
    ///     Use for UI/UX purposes (e.g., showing "approaching limit" warnings) or soft-limit checks.
    ///     </para>
    ///     <para>
    ///     <b>DO NOT</b> use this for authoritative enforcement - callers can ignore the result.
    ///     For atomic enforcement, use <see cref="TryAtomicConsumeAsync"/> instead.
    ///     </para>
    /// </summary>
    Task<ResourceLimitCheckResponse> CheckLimitsAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check limits for multiple resource types
    /// </summary>
    Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(Guid tenantId, Dictionary<ResourceUsageType, long> requestedAmounts, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Attempt to consume resources with atomic enforcement.
    ///     <para>
    ///     <b>AUTHORITATIVE:</b> This method delegates to <see cref="TryAtomicConsumeAsync"/>
    ///     for atomic check-and-increment operation that is safe under concurrent access.
    ///     </para>
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="type">Resource usage type</param>
    /// <param name="amount">Amount to consume</param>
    /// <param name="userId">Optional user ID for tracking</param>
    /// <param name="source">Optional source identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing success status and current quota info</returns>
    Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(Guid tenantId, ResourceUsageType type, long amount = 1, Guid? userId = null, string? source = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Atomically attempts to consume resources with optimistic concurrency.
    ///     <para>
    ///     <b>AUTHORITATIVE:</b> This is the core atomic operation for quota enforcement.
    ///     Uses RowVersion concurrency with retry logic to prevent race conditions.
    ///     </para>
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="type">Resource usage type</param>
    /// <param name="amount">Amount to consume</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     Tuple containing: Success (true if consume succeeded), CurrentUsage, HardLimit.
    ///     Returns (true, 0, null) if no quota exists (unlimited).
    ///     Returns (false, currentUsage, hardLimit) if would exceed hard limit.
    /// </returns>
    Task<(bool Success, long CurrentUsage, long? HardLimit)> TryAtomicConsumeAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        CancellationToken cancellationToken = default);

    // Analytics and Reporting
    /// <summary>
    ///     Get detailed usage information for a specific resource type
    /// </summary>
    Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenants that have exceeded their limits
    /// </summary>
    Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Decrement resource usage (e.g., when a resource is deleted).
    ///     Ensures usage never goes negative.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="type">Resource usage type</param>
    /// <param name="amount">Amount to decrement (default: 1)</param>
    /// <param name="userId">Optional user ID for tracking</param>
    /// <param name="source">Optional source identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if decrement was applied</returns>
    Task<bool> DecrementUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        Guid? userId = null,
        string? source = null,
        CancellationToken cancellationToken = default);

    // Maintenance
    /// <summary>
    ///     Reset quotas that are due for reset based on their period
    /// </summary>
    Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clean up old usage records
    /// </summary>
    Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Recalculate current usage from usage records
    /// </summary>
    Task<bool> RecalculateUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);
}

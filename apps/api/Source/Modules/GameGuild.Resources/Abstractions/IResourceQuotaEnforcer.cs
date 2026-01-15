namespace GameGuild.Resources;

/// <summary>
///     Enforcement operations for resource quota consumption.
///     Use this interface for operations that consume or check quota limits.
/// </summary>
/// <remarks>
///     Part of the ISP-compliant split of IResourceQuotaService.
///     This is the primary interface for ResourceQuotaBehavior and command handlers.
/// </remarks>
public interface IResourceQuotaEnforcer
{
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
}

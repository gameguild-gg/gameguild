namespace GameGuild.Modules.Tenants.Abstractions;

/// <summary>
///     Service for tracking and managing tenant resource usage
/// </summary>
public interface IUsageTrackingService
{
    /// <summary>
    ///     Track resource usage for a tenant
    /// </summary>
    Task<UsageTracking> TrackUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        long amount,
        string? customResourceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increment existing usage by amount
    /// </summary>
    Task<UsageTracking> IncrementUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        long amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if tenant has exceeded usage limits
    /// </summary>
    Task<bool> CheckLimitExceededAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get current usage for a tenant and resource type
    /// </summary>
    Task<UsageTracking?> GetUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all usage entries for a tenant
    /// </summary>
    Task<IReadOnlyList<UsageTracking>> GetAllUsageAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reset usage to zero and start new period
    /// </summary>
    Task ResetUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update usage limit for a resource
    /// </summary>
    Task UpdateLimitAsync(
        Guid tenantId,
        ResourceType resourceType,
        long newLimit,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if usage is within limit (with optional buffer percentage)
    /// </summary>
    Task<bool> IsWithinLimitAsync(
        Guid tenantId,
        ResourceType resourceType,
        decimal bufferPercentage = 0,
        CancellationToken cancellationToken = default);
}

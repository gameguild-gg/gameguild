
namespace GameGuild.Resources;

/// <summary>
///     Interface for usage tracking and monitoring services
/// </summary>
public interface IUsageService
{
    /// <summary>
    ///     Get current usage for a tenant
    /// </summary>
    Task<TenantResourceUsage> GetCurrentUsageAsync(Guid tenantId);

    /// <summary>
    ///     Get usage history for a tenant
    /// </summary>
    Task<IEnumerable<ResourceUsageHistory>> GetUsageHistoryAsync(Guid tenantId, int months);

    /// <summary>
    ///     Track resource usage
    /// </summary>
    Task TrackUsageAsync(Guid tenantId, string resourceType, int amount);

    /// <summary>
    ///     Check if tenant has exceeded limits
    /// </summary>
    Task<bool> IsWithinLimitsAsync(Guid tenantId, string resourceType, int requestedAmount);
}

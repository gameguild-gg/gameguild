using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Abstractions;

/// <summary>
///     Service for tracking and managing tenant usage data
/// </summary>
public interface IUsageTrackingService
{
    /// <summary>
    ///     Track usage data for a tenant
    /// </summary>
    /// <param name="usageTracking">Usage tracking data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created usage tracking record</returns>
    Task<Guid> TrackUsageAsync(UsageTracking usageTracking, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get usage data for a tenant within a date range
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="resourceType">Optional resource type filter</param>
    /// <param name="actionType">Optional action type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of usage tracking records</returns>
    Task<List<UsageTracking>> GetUsageAsync(Guid tenantId, DateTime startDate, DateTime endDate, string? resourceType = null, string? actionType = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get usage summary for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage summary</returns>
    Task<UsageSummary> GetUsageSummaryAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete usage data older than the specified date
    /// </summary>
    /// <param name="cutoffDate">Cutoff date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    Task<int> CleanupOldUsageDataAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}

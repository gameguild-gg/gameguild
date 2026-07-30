namespace GameGuild.Features;

/// <summary>
///     Repository interface for feature flag analytics and usage tracking operations.
///     Follows Interface Segregation Principle (ISP) by separating analytics concerns from CRUD operations.
/// </summary>
public interface IFeatureFlagAnalyticsRepository
{
    /// <summary>
    ///     Records feature flag usage for analytics tracking
    /// </summary>
    /// <param name="usage">The usage record to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordUsageAsync(FeatureFlagUsage usage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets usage analytics for a specific feature flag within a date range
    /// </summary>
    /// <param name="featureKey">The feature flag key</param>
    /// <param name="startDate">Start date of the analytics period</param>
    /// <param name="endDate">End date of the analytics period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage records for the specified period</returns>
    Task<IEnumerable<FeatureFlagUsage>> GetUsageAnalyticsAsync(string featureKey, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets usage analytics for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="startDate">Start date of the analytics period</param>
    /// <param name="endDate">End date of the analytics period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage records for the specified tenant and period</returns>
    Task<IEnumerable<FeatureFlagUsage>> GetUsageByTenantAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets usage analytics for a specific user
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="startDate">Start date of the analytics period</param>
    /// <param name="endDate">End date of the analytics period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of usage records for the specified user and period</returns>
    Task<IEnumerable<FeatureFlagUsage>> GetUsageByUserAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets aggregated usage statistics for a feature flag
    /// </summary>
    /// <param name="featureKey">The feature flag key</param>
    /// <param name="startDate">Start date of the analytics period</param>
    /// <param name="endDate">End date of the analytics period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated statistics including total access count, enabled count, etc.</returns>
    Task<FeatureFlagUsageStats> GetAggregatedStatsAsync(string featureKey, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the most frequently accessed feature flags
    /// </summary>
    /// <param name="topCount">Number of top feature flags to return</param>
    /// <param name="startDate">Start date of the analytics period</param>
    /// <param name="endDate">End date of the analytics period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of feature keys ordered by access count</returns>
    Task<IEnumerable<string>> GetMostAccessedFeaturesAsync(int topCount, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Bulk records multiple usage entries for better performance
    /// </summary>
    /// <param name="usageRecords">Collection of usage records to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordUsageBulkAsync(IEnumerable<FeatureFlagUsage> usageRecords, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes old usage records before a specific date (for data retention policies)
    /// </summary>
    /// <param name="beforeDate">Delete records older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    Task<int> PurgeOldUsageRecordsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Exports analytics data for external reporting
    /// </summary>
    /// <param name="featureKeys">Feature keys to export analytics for</param>
    /// <param name="startDate">Start date for analytics period</param>
    /// <param name="endDate">End date for analytics period</param>
    /// <param name="format">Export format (csv, json, etc.)</param>
    /// <param name="includeDetails">Whether to include detailed records</param>
    /// <param name="groupBy">Grouping criteria</param>
    /// <param name="environment">Target environment</param>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported analytics result</returns>
    Task<AnalyticsExportResult> ExportAnalyticsAsync(
        IEnumerable<string>? featureKeys,
        DateTime? startDate,
        DateTime? endDate,
        string format,
        bool includeDetails,
        string? groupBy,
        string? environment,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    );
}

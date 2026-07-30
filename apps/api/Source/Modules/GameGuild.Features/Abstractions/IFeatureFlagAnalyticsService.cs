namespace GameGuild.Features;

/// <summary>
///     Service interface for feature flag analytics and usage tracking.
///     Follows Interface Segregation Principle (ISP) by focusing only on analytics operations.
/// </summary>
/// <remarks>
///     This interface should be used when you need to track feature usage, retrieve analytics,
///     or generate reports. For evaluation, use IFeatureFlagEvaluationService.
/// </remarks>
public interface IFeatureFlagAnalyticsService
{
    /// <summary>
    ///     Records feature flag usage for analytics
    /// </summary>
    /// <param name="featureKey">The feature flag key</param>
    /// <param name="context">The evaluation context</param>
    /// <param name="wasEnabled">Whether the feature was enabled</param>
    /// <param name="value">The value returned (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordUsageAsync(string featureKey, FeatureContext context, bool wasEnabled, string? value = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets analytics for a specific feature flag
    /// </summary>
    /// <param name="featureKey">The feature flag key</param>
    /// <param name="startDate">Start date of the analytics period (optional)</param>
    /// <param name="endDate">End date of the analytics period (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics data for the specified feature</returns>
    Task<FeatureFlagAnalytics> GetAnalyticsAsync(string featureKey, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets analytics for multiple features
    /// </summary>
    /// <param name="featureKeys">Collection of feature flag keys</param>
    /// <param name="startDate">Start date of the analytics period (optional)</param>
    /// <param name="endDate">End date of the analytics period (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping feature keys to their analytics</returns>
    Task<IDictionary<string, FeatureFlagAnalytics>> GetBulkAnalyticsAsync(IEnumerable<string> featureKeys, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets analytics for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="startDate">Start date of the analytics period (optional)</param>
    /// <param name="endDate">End date of the analytics period (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tenant-specific analytics data</returns>
    Task<TenantFeatureAnalytics> GetTenantAnalyticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the most frequently accessed features
    /// </summary>
    /// <param name="topCount">Number of top features to return</param>
    /// <param name="startDate">Start date of the analytics period (optional)</param>
    /// <param name="endDate">End date of the analytics period (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of feature keys ordered by access count</returns>
    Task<IEnumerable<FeatureUsageRanking>> GetTopFeaturesAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets real-time usage statistics
    /// </summary>
    /// <param name="featureKey">The feature flag key (optional, null for all features)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time usage statistics</returns>
    Task<RealtimeUsageStats> GetRealtimeStatsAsync(string? featureKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Exports analytics data for reporting
    /// </summary>
    /// <param name="request">Export request with filters and format options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported analytics data in the requested format</returns>
    Task<AnalyticsExportResult> ExportAnalyticsAsync(AnalyticsExportRequest request, CancellationToken cancellationToken = default);
}

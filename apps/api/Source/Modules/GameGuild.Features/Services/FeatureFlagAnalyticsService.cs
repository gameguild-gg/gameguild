using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Features;

/// <summary>
///     Service for feature flag analytics and usage tracking.
///     Implements IFeatureFlagAnalyticsService following the Interface Segregation Principle.
/// </summary>
public class FeatureFlagAnalyticsService(
    IFeatureFlagQueryRepository queryRepository,
    IFeatureFlagAnalyticsRepository analyticsRepository,
    ILogger<FeatureFlagAnalyticsService> logger,
    IOptions<FeatureFlagOptions> options
) : IFeatureFlagAnalyticsService
{
    private readonly IFeatureFlagAnalyticsRepository _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));

    private readonly ILogger<FeatureFlagAnalyticsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly FeatureFlagOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly IFeatureFlagQueryRepository _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));

    /// <inheritdoc />
    public async Task RecordUsageAsync(string featureKey, FeatureContext context, bool wasEnabled, string? value = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.EnableAnalytics) { return; }

        try
        {
            var featureFlag = await _queryRepository.GetByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null)
            {
                _logger.LogWarning("Cannot record usage for unknown feature '{FeatureKey}'", featureKey);

                return;
            }

            var usage = new FeatureFlagUsage
            {
                FeatureFlagId = featureFlag.Id,
                TenantId = context.TenantId,
                UserId = context.UserId,
                Environment = context.Environment,
                WasEnabled = wasEnabled,
                ReturnedValue = value,
                FirstAccessAt = SystemClock.UtcNow,
                LastAccessAt = SystemClock.UtcNow,
                AccessCount = 1,
                ContextData = JsonSerializer.Serialize(new { context.UserAgent, context.IpAddress, context.Country, context.RequestTime })
            };

            await _analyticsRepository.RecordUsageAsync(usage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Don't throw - analytics shouldn't break the application
            _logger.LogWarning(ex, "Failed to record usage for feature '{FeatureKey}'", featureKey);
        }
    }

    /// <inheritdoc />
    public async Task<FeatureFlagAnalytics> GetAnalyticsAsync(string featureKey, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);

        var start = startDate ?? SystemClock.UtcNow.AddDays(-30);
        var end = endDate ?? SystemClock.UtcNow;

        ValidateDateRange(start, end);

        try
        {
            // Get usage records for potential future detailed analytics
            _ = await _analyticsRepository.GetUsageAnalyticsAsync(featureKey, start, end, cancellationToken).ConfigureAwait(false);

            var stats = await _analyticsRepository.GetAggregatedStatsAsync(featureKey, start, end, cancellationToken).ConfigureAwait(false);

            return new FeatureFlagAnalytics
            {
                FeatureKey = featureKey,
                TotalAccesses = stats.TotalAccessCount,
                EnabledAccesses = stats.EnabledCount,
                DisabledAccesses = stats.DisabledCount,
                EnabledPercentage = stats.EnabledPercentage,
                UniqueUsers = stats.UniqueUserCount,
                UniqueTenants = stats.UniqueTenantCount,
                FirstAccess = stats.FirstAccessDate ?? DateTime.MinValue,
                LastAccess = stats.LastAccessDate ?? DateTime.MinValue,
                PeriodStart = start,
                PeriodEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics for feature '{FeatureKey}'", featureKey);

            return CreateEmptyAnalytics(featureKey, start, end);
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, FeatureFlagAnalytics>> GetBulkAnalyticsAsync(IEnumerable<string> featureKeys, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(featureKeys);

        var keysList = featureKeys.ToList();

        if (!keysList.Any()) { return new Dictionary<string, FeatureFlagAnalytics>(); }

        var start = startDate ?? SystemClock.UtcNow.AddDays(-30);
        var end = endDate ?? SystemClock.UtcNow;

        ValidateDateRange(start, end);

        try
        {
            var tasks = keysList.Select(async key =>
                {
                    var analytics = await GetAnalyticsAsync(key, start, end, cancellationToken).ConfigureAwait(false);

                    return new { Key = key, Analytics = analytics };
                }
            );

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.ToDictionary(r => r.Key, r => r.Analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bulk analytics");

            return new Dictionary<string, FeatureFlagAnalytics>();
        }
    }

    /// <inheritdoc />
    public async Task<TenantFeatureAnalytics> GetTenantAnalyticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? SystemClock.UtcNow.AddDays(-30);
        var end = endDate ?? SystemClock.UtcNow;

        ValidateDateRange(start, end);

        try
        {
            var usageRecords = await _analyticsRepository.GetUsageByTenantAsync(tenantId, start, end, cancellationToken).ConfigureAwait(false);

            var usageList = usageRecords.ToList();

            var analytics = new TenantFeatureAnalytics
            {
                TenantId = tenantId,
                TotalFeaturesAccessed = usageList.Select(u => u.FeatureFlagId).Distinct().Count(),
                EnabledFeaturesCount = usageList.Count(u => u.WasEnabled),
                DisabledFeaturesCount = usageList.Count(u => !u.WasEnabled),
                TotalAccessCount = usageList.Sum(u => u.AccessCount),
                FirstAccessDate = usageList.Any() ? usageList.Min(u => u.FirstAccessAt) : null,
                LastAccessDate = usageList.Any() ? usageList.Max(u => u.LastAccessAt) : null,
                AccessByEnvironment = usageList.GroupBy(u => u.Environment).ToDictionary(g => g.Key, g => g.Sum(u => u.AccessCount))
            };

            // Get top features for this tenant
            var featureGroups = usageList.GroupBy(u => u.FeatureFlagId).OrderByDescending(g => g.Sum(u => u.AccessCount)).Take(10);

            var topFeatures = new List<FeatureUsageRanking>();
            var rank = 1;

            foreach (var group in featureGroups)
            {
                var featureFlag = await _queryRepository.GetByIdAsync(group.Key, cancellationToken).ConfigureAwait(false);

                if (featureFlag != null)
                {
                    topFeatures.Add(
                        new FeatureUsageRanking
                        {
                            FeatureKey = featureFlag.Key,
                            AccessCount = group.Sum(u => u.AccessCount),
                            EnabledCount = group.Count(u => u.WasEnabled),
                            DisabledCount = group.Count(u => !u.WasEnabled),
                            UniqueUserCount = group.Select(u => u.UserId).Distinct().Count(),
                            UniqueTenantCount = 1, // Single tenant
                            Rank = rank++
                        }
                    );
                }
            }

            analytics.TopFeatures = topFeatures;

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant analytics for tenant {TenantId}", tenantId);

            return new TenantFeatureAnalytics { TenantId = tenantId };
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureUsageRanking>> GetTopFeaturesAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        if (topCount <= 0) { throw new ArgumentOutOfRangeException(nameof(topCount), "Top count must be greater than 0"); }

        var start = startDate ?? SystemClock.UtcNow.AddDays(-30);
        var end = endDate ?? SystemClock.UtcNow;

        ValidateDateRange(start, end);

        try
        {
            var topFeatureKeys = await _analyticsRepository.GetMostAccessedFeaturesAsync(topCount, start, end, cancellationToken).ConfigureAwait(false);

            var rankings = new List<FeatureUsageRanking>();
            var rank = 1;

            foreach (var featureKey in topFeatureKeys)
            {
                var stats = await _analyticsRepository.GetAggregatedStatsAsync(featureKey, start, end, cancellationToken).ConfigureAwait(false);

                rankings.Add(
                    new FeatureUsageRanking
                    {
                        FeatureKey = featureKey,
                        AccessCount = stats.TotalAccessCount,
                        EnabledCount = stats.EnabledCount,
                        DisabledCount = stats.DisabledCount,
                        UniqueUserCount = stats.UniqueUserCount,
                        UniqueTenantCount = stats.UniqueTenantCount,
                        Rank = rank++
                    }
                );
            }

            return rankings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top features");

            return [];
        }
    }

    /// <inheritdoc />
    public async Task<RealtimeUsageStats> GetRealtimeStatsAsync(string? featureKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = SystemClock.UtcNow;
            var oneHourAgo = now.AddHours(-1);
            var startOfDay = now.Date;

            // For now, use aggregated stats as a proxy for realtime
            // In production, this would query a time-series database or cache

            var hourlyStats = featureKey != null ? await _analyticsRepository.GetAggregatedStatsAsync(featureKey, oneHourAgo, now, cancellationToken) : null;

            var dailyStats = featureKey != null ? await _analyticsRepository.GetAggregatedStatsAsync(featureKey, startOfDay, now, cancellationToken) : null;

            return new RealtimeUsageStats
            {
                Timestamp = now,
                EvaluationsLastMinute = 0, // Would require time-series data
                EvaluationsLastHour = hourlyStats?.TotalAccessCount ?? 0,
                EvaluationsToday = dailyStats?.TotalAccessCount ?? 0,
                ActiveFeatureCount = 0, // Would require active tracking
                ErrorRate = 0, // Would require error tracking
                AverageLatencyMs = 0, // Would require performance monitoring
                CacheHitRate = 0 // Would require cache metrics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving realtime stats");

            return new RealtimeUsageStats { Timestamp = SystemClock.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<AnalyticsExportResult> ExportAnalyticsAsync(AnalyticsExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var start = request.StartDate ?? SystemClock.UtcNow.AddDays(-30);
        var end = request.EndDate ?? SystemClock.UtcNow;

        ValidateDateRange(start, end);

        try
        {
            // Get data to export - use provided keys or get all if empty
            var featureKeys = request.FeatureKeys.Count > 0 
                ? request.FeatureKeys 
                : (await _queryRepository.GetAllAsync(cancellationToken)).Select(f => f.Key).ToList();

            var analyticsData = await GetBulkAnalyticsAsync(featureKeys, start, end, cancellationToken).ConfigureAwait(false);

            // Generate export based on format
            return request.Format.ToLowerInvariant() switch
            {
                "json" => ExportAsJson(analyticsData, request),
                "csv" => ExportAsCsv(analyticsData, request),
                _ => throw new NotSupportedException($"Export format '{request.Format}' is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics");

            throw;
        }
    }

    #region Private Helper Methods

    private static void ValidateDateRange(DateTime start, DateTime end)
    {
        if (start > end) { throw new ArgumentException("Start date must be before end date"); }

        if ((end - start).TotalDays > 365) { throw new ArgumentException("Date range cannot exceed 365 days"); }
    }

    private static FeatureFlagAnalytics CreateEmptyAnalytics(string featureKey, DateTime start, DateTime end)
    {
        return new FeatureFlagAnalytics { FeatureKey = featureKey, TotalAccesses = 0, EnabledAccesses = 0, DisabledAccesses = 0, EnabledPercentage = 0, PeriodStart = start, PeriodEnd = end };
    }

    private static AnalyticsExportResult ExportAsJson(IDictionary<string, FeatureFlagAnalytics> analyticsData, AnalyticsExportRequest request)
    {
        var json = JsonSerializer.Serialize(analyticsData, new JsonSerializerOptions { WriteIndented = request.IncludeDetails });

        return new AnalyticsExportResult
        {
            Content = Encoding.UTF8.GetBytes(json),
            ContentType = "application/json",
            FileName = $"feature-analytics-{SystemClock.UtcNow:yyyyMMdd-HHmmss}.json",
            RecordCount = analyticsData.Count,
            GeneratedAt = SystemClock.UtcNow
        };
    }

    private static AnalyticsExportResult ExportAsCsv(IDictionary<string, FeatureFlagAnalytics> analyticsData, AnalyticsExportRequest _)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("FeatureKey,TotalAccesses,EnabledAccesses,DisabledAccesses,EnabledPercentage,FirstAccess,LastAccess");

        // Data rows
        foreach (var kvp in analyticsData)
        {
            var analytics = kvp.Value;

            csv.AppendLine(
                $"{analytics.FeatureKey},{analytics.TotalAccesses},{analytics.EnabledAccesses}," +
                $"{analytics.DisabledAccesses},{analytics.EnabledPercentage:F2}," +
                $"{analytics.FirstAccess:yyyy-MM-dd HH:mm:ss},{analytics.LastAccess:yyyy-MM-dd HH:mm:ss}"
            );
        }

        return new AnalyticsExportResult
        {
            Content = Encoding.UTF8.GetBytes(csv.ToString()),
            ContentType = "text/csv",
            FileName = $"feature-analytics-{SystemClock.UtcNow:yyyyMMdd-HHmmss}.csv",
            RecordCount = analyticsData.Count,
            GeneratedAt = SystemClock.UtcNow
        };
    }

    #endregion
}

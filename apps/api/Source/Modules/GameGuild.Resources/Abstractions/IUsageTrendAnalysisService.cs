
namespace GameGuild.Resources;

/// <summary>
///     Service for usage trend analysis and forecasting
/// </summary>
public interface IUsageTrendAnalysisService
{
    /// <summary>
    ///     Analyze usage trends for a tenant and resource type
    /// </summary>
    Task<ResourceUsageTrend> AnalyzeTrendAsync(Guid tenantId, ResourceUsageType type, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all trends for a tenant
    /// </summary>
    Task<IEnumerable<ResourceUsageTrend>> GetTenantTrendsAsync(Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Detect anomalies in usage patterns
    /// </summary>
    Task<IEnumerable<ResourceUsageTrend>> DetectAnomaliesAsync(Guid tenantId, ResourceUsageType? type = null, int lookbackDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Forecast future usage based on historical trends
    /// </summary>
    Task<long> ForecastUsageAsync(Guid tenantId, ResourceUsageType type, DateTime targetDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get usage patterns by classification
    /// </summary>
    Task<Dictionary<string, int>> GetPatternDistributionAsync(Guid? tenantId = null, ResourceUsageType? type = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculate growth rate for a resource type
    /// </summary>
    Task<decimal> CalculateGrowthRateAsync(Guid tenantId, ResourceUsageType type, int periodDays = 30, CancellationToken cancellationToken = default);

    // PLANNED: Integration with ML/AI module for advanced pattern recognition (depends on GameGuild.ML)
    // PLANNED: Integration with Monitoring module for real-time alerts (depends on GameGuild.Monitoring)
}

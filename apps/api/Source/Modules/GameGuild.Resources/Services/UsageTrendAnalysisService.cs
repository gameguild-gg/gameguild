using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources.Services;

/// <summary>
///     Implementation of usage trend analysis and forecasting
/// </summary>
public class UsageTrendAnalysisService(IResourceUsageTrendRepository trendRepository, IUsageRecordRepository usageRepository, ILogger<UsageTrendAnalysisService> logger) : IUsageTrendAnalysisService
{
    public async Task<ResourceUsageTrend> AnalyzeTrendAsync(Guid tenantId, ResourceUsageType type, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var usageRecords = await usageRepository.GetByTenantAsync(tenantId, type, periodStart, periodEnd, cancellationToken);

        var recordsList = usageRecords.ToList();

        if (recordsList.Count == 0)
        {
            logger.LogWarning("No usage records found for tenant {TenantId}, type {Type} in period {Start} to {End}", tenantId, type, periodStart, periodEnd);

            var emptyTrend = new ResourceUsageTrend
            {
                Type = type, PeriodStart = periodStart, PeriodEnd = periodEnd, AverageUsage = 0, MinUsage = 0, MaxUsage = 0, StandardDeviation = 0, GrowthRate = 0, AnomalyCount = 0, Pattern = "Insufficient Data"
            };
            emptyTrend.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });

            return emptyTrend;
        }

        var usageAmounts = recordsList.Select(r => r.UsageAmount).ToList();

        var averageUsage = usageAmounts.Average();
        var minUsage = usageAmounts.Min();
        var maxUsage = usageAmounts.Max();
        var stdDev = CalculateStandardDeviation(usageAmounts);

        // Calculate growth rate (compare first half vs second half)
        var growthRate = CalculateGrowthRate(recordsList, periodStart, periodEnd);

        // Detect anomalies
        var anomalyCount = DetectAnomalyCount(usageAmounts, averageUsage, stdDev);

        // Classify pattern
        var pattern = ClassifyPattern(growthRate, stdDev, averageUsage);

        var trend = new ResourceUsageTrend
        {
            Type = type,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AverageUsage = (long) averageUsage,
            MinUsage = minUsage,
            MaxUsage = maxUsage,
            StandardDeviation = (double) stdDev,
            GrowthRate = (double) growthRate,
            AnomalyCount = anomalyCount,
            Pattern = pattern
        };
        trend.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });

        var savedTrend = await trendRepository.AddAsync(trend, cancellationToken);

        logger.LogInformation("Analyzed trend for tenant {TenantId}, type {Type}: Pattern={Pattern}, Growth={Growth:P}", tenantId, type, pattern, growthRate);

        return savedTrend;
    }

    public async Task<IEnumerable<ResourceUsageTrend>> GetTenantTrendsAsync(Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        return await trendRepository.GetByTenantAsync(tenantId, null, fromDate, toDate, cancellationToken);
    }

    public async Task<IEnumerable<ResourceUsageTrend>> DetectAnomaliesAsync(Guid tenantId, ResourceUsageType? type = null, int lookbackDays = 30, CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.AddDays(-lookbackDays);

        var trends = await trendRepository.GetByTenantAsync(tenantId, type, fromDate, null, cancellationToken);

        return trends.Where(t => t.AnomalyCount > 0 || t.IsAnomaly((long) t.AverageUsage, t.StandardDeviation));
    }

    public async Task<long> ForecastUsageAsync(Guid tenantId, ResourceUsageType type, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        // Get recent trends (last 90 days)
        var fromDate = DateTime.UtcNow.AddDays(-90);

        var trends = await trendRepository.GetByTenantAsync(tenantId, type, fromDate, null, cancellationToken);

        var trendsList = trends.ToList();

        if (trendsList.Count == 0)
        {
            logger.LogWarning("No trends found for forecasting tenant {TenantId}, type {Type}", tenantId, type);

            return 0;
        }

        // Use average growth rate and last known average
        var avgGrowthRate = trendsList.Average(t => t.GrowthRate);
        var lastTrend = trendsList.OrderByDescending(t => t.PeriodEnd).First();

        var daysToForecast = (targetDate - lastTrend.PeriodEnd).TotalDays;
        var forecast = lastTrend.ForecastNextPeriod();

        logger.LogInformation("Forecast for tenant {TenantId}, type {Type} on {Date}: {Forecast}", tenantId, type, targetDate, forecast);

        return (long) forecast;
    }

    public async Task<Dictionary<string, int>> GetPatternDistributionAsync(Guid? tenantId = null, ResourceUsageType? type = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<ResourceUsageTrend> trends;

        if (tenantId.HasValue) { trends = await trendRepository.GetByTenantAsync(tenantId.Value, type, null, null, cancellationToken); }
        else
        {
            // Get all trends - would need an additional repository method
            // For now, return empty result
            logger.LogWarning("GetPatternDistributionAsync called without tenant ID - not yet supported");

            return new Dictionary<string, int>();
        }

        return trends.GroupBy(t => t.Pattern).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<decimal> CalculateGrowthRateAsync(Guid tenantId, ResourceUsageType type, int periodDays = 30, CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-periodDays);

        var usageRecords = await usageRepository.GetByTenantAsync(tenantId, type, startDate, endDate, cancellationToken);

        var recordsList = usageRecords.OrderBy(r => r.PeriodStart).ToList();

        if (recordsList.Count < 2) return 0;

        return CalculateGrowthRate(recordsList, startDate, endDate);
    }

    private static decimal CalculateStandardDeviation(List<long> values)
    {
        if (values.Count <= 1) return 0;

        var average = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));

        return (decimal) Math.Sqrt(sumOfSquares / values.Count);
    }

    private static decimal CalculateGrowthRate(List<UsageRecord> records, DateTime periodStart, DateTime periodEnd)
    {
        if (records.Count < 2) return 0;

        var midpoint = periodStart.AddTicks((periodEnd - periodStart).Ticks / 2);

        var firstHalf = records.Where(r => r.PeriodStart < midpoint).ToList();
        var secondHalf = records.Where(r => r.PeriodStart >= midpoint).ToList();

        if (firstHalf.Count == 0 || secondHalf.Count == 0) return 0;

        var firstAvg = firstHalf.Average(r => r.UsageAmount);
        var secondAvg = secondHalf.Average(r => r.UsageAmount);

        if (firstAvg == 0) return 0;

        return (decimal) ((secondAvg - firstAvg) / firstAvg);
    }

    private static int DetectAnomalyCount(List<long> values, double average, decimal stdDev)
    {
        if (stdDev == 0) return 0;

        var threshold = 2.0; // 2 standard deviations

        return values.Count(v => Math.Abs(v - average) > (double) stdDev * threshold);
    }

    private static string ClassifyPattern(decimal growthRate, decimal stdDev, double average)
    {
        // High variability
        if (average > 0 && stdDev / (decimal) average > 0.5m) return "Volatile";

        // Growth patterns
        if (growthRate > 0.2m) return "Rapid Growth";
        if (growthRate > 0.05m) return "Growing";
        if (growthRate < -0.2m) return "Rapid Decline";
        if (growthRate < -0.05m) return "Declining";

        // Stable pattern
        return "Stable";
    }

    // TODO: Integration with ML/AI module for advanced pattern recognition
    // TODO: Integration with Monitoring module for real-time alerts
}

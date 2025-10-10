namespace GameGuild.Modules.Resources.Queries;
using GameGuild.Database;

/// <summary>
///     Query to analyze usage trends for a tenant and resource type
/// </summary>
public record AnalyzeUsageTrendsQuery(
    Guid TenantId,
    ResourceUsageType ResourceType,
    DateTime StartDate,
    DateTime EndDate,
    int? MinDataPoints = 10
) : IRequest<Result<UsageTrendAnalysisResponse>>;

public record UsageTrendAnalysisResponse(
    ResourceUsageType ResourceType,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    double AverageUsage,
    long MinUsage,
    long MaxUsage,
    double StandardDeviation,
    double GrowthRate,
    int AnomalyCount,
    string Pattern,
    double PatternConfidence,
    double ForecastedNextPeriod,
    List<UsageAnomalyDto> Anomalies
);

public record UsageAnomalyDto(
    DateTime Timestamp,
    long UsageValue,
    double ZScore,
    string Reason
);

/// <summary>
///     Handler for usage trend analysis
/// </summary>
public class AnalyzeUsageTrendsHandler : IRequestHandler<AnalyzeUsageTrendsQuery, Result<UsageTrendAnalysisResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyzeUsageTrendsHandler> _logger;

    public AnalyzeUsageTrendsHandler(ApplicationDbContext context, ILogger<AnalyzeUsageTrendsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UsageTrendAnalysisResponse>> Handle(AnalyzeUsageTrendsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get usage records for the period
            var usageRecords = await _context.Set<ResourceUsageRecord>()
                .Where(r => r.TenantId == request.TenantId && 
                           r.Type == request.ResourceType &&
                           r.Timestamp >= request.StartDate && 
                           r.Timestamp <= request.EndDate)
                .OrderBy(r => r.Timestamp)
                .ToListAsync(cancellationToken);

            if (usageRecords.Count < (request.MinDataPoints ?? 10))
            {
                return Result<UsageTrendAnalysisResponse>.Failure("Insufficient data points for trend analysis");
            }

            // Calculate statistics
            var usageValues = usageRecords.Select(r => r.Amount).ToList();
            var average = usageValues.Average();
            var min = usageValues.Min();
            var max = usageValues.Max();
            var stdDev = CalculateStandardDeviation(usageValues, average);

            // Detect anomalies
            var anomalies = DetectAnomalies(usageRecords, average, stdDev);

            // Calculate growth rate
            var growthRate = CalculateGrowthRate(usageRecords);

            // Detect pattern
            var (pattern, confidence) = DetectPattern(usageRecords, growthRate, anomalies.Count, stdDev);

            // Forecast next period
            var forecast = average * (1 + growthRate / 100);

            // Store trend analysis
            var trend = new ResourceUsageTrend
            {
                TenantId = request.TenantId,
                ResourceType = request.ResourceType,
                PeriodStart = request.StartDate,
                PeriodEnd = request.EndDate,
                AverageUsage = average,
                MinUsage = min,
                MaxUsage = max,
                StandardDeviation = stdDev,
                GrowthRate = growthRate,
                AnomalyCount = anomalies.Count,
                PeakUsageTime = usageRecords.MaxBy(r => r.Amount)?.Timestamp,
                Pattern = pattern,
                PatternConfidence = confidence
            };

            _context.Set<ResourceUsageTrend>().Add(trend);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new UsageTrendAnalysisResponse(
                request.ResourceType,
                request.StartDate,
                request.EndDate,
                average,
                min,
                max,
                stdDev,
                growthRate,
                anomalies.Count,
                pattern,
                confidence,
                forecast,
                anomalies
            );

            return Result<UsageTrendAnalysisResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing usage trends for tenant {TenantId}, resource {ResourceType}", 
                request.TenantId, request.ResourceType);
            return Result<UsageTrendAnalysisResponse>.Failure($"Error analyzing usage trends: {ex.Message}");
        }
    }

    private static double CalculateStandardDeviation(List<long> values, double average)
    {
        if (values.Count == 0) return 0;

        var sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    private static List<UsageAnomalyDto> DetectAnomalies(List<ResourceUsageRecord> records, double average, double stdDev)
    {
        var anomalies = new List<UsageAnomalyDto>();
        
        if (stdDev == 0) return anomalies;

        foreach (var record in records)
        {
            var zScore = Math.Abs((record.Amount - average) / stdDev);
            
            if (zScore > 2.0) // Anomaly threshold
            {
                anomalies.Add(new UsageAnomalyDto(
                    record.Timestamp,
                    record.Amount,
                    zScore,
                    zScore > 3.0 ? "Severe anomaly" : "Moderate anomaly"
                ));
            }
        }

        return anomalies;
    }

    private static double CalculateGrowthRate(List<ResourceUsageRecord> records)
    {
        if (records.Count < 2) return 0;

        var midPoint = records.Count / 2;
        var firstHalf = records.Take(midPoint).Average(r => r.Amount);
        var secondHalf = records.Skip(midPoint).Average(r => r.Amount);

        if (firstHalf == 0) return 0;

        return ((secondHalf - firstHalf) / firstHalf) * 100;
    }

    private static (string Pattern, double Confidence) DetectPattern(
        List<ResourceUsageRecord> records, 
        double growthRate, 
        int anomalyCount,
        double stdDev)
    {
        // Simple pattern detection logic
        if (anomalyCount > records.Count * 0.1)
            return ("Anomalous", 0.8);

        if (stdDev > records.Average(r => r.Amount) * 0.5)
            return ("Volatile", 0.7);

        if (Math.Abs(growthRate) < 5)
            return ("Steady", 0.9);

        if (growthRate > 20)
            return ("Growing", 0.85);

        if (growthRate < -20)
            return ("Declining", 0.85);

        return ("Variable", 0.6);
    }
}

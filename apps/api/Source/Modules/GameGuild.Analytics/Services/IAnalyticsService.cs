namespace GameGuild.Analytics;

public record AnalyticsEventDto(Guid Id, string EventName, string? Properties, Guid? UserId, string? SessionId, DateTime Timestamp);
public record KpiResultDto(string KpiName, double Value, DateTime StartDate, DateTime EndDate, DateTime CalculatedAt);
public record TimeSeriesDataPointDto(DateTime Timestamp, double Value);
public record AggregationResultDto(Dictionary<string, string> GroupKey, double Value);
public record FunnelStepDto(string StepName, int UserCount, double DropOffRate);
public record FunnelAnalysisResultDto(List<FunnelStepDto> Steps, DateTime StartDate, DateTime EndDate, int TotalUsers);

public interface IAnalyticsService
{
    Task<AnalyticsEventDto> TrackEventAsync(string eventName, string? propertiesJson, Guid? userId, Guid? tenantId, CancellationToken ct = default);
    Task TrackEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default);
    Task<KpiResultDto> CalculateKpiAsync(string kpiName, DateTime startDate, DateTime endDate, Guid? tenantId, CancellationToken ct = default);
    Task<List<TimeSeriesDataPointDto>> GetTimeSeriesAsync(string eventName, DateTime startDate, DateTime endDate, TimeSeriesGranularity granularity, Guid? tenantId, CancellationToken ct = default);
    Task<List<AggregationResultDto>> AggregateEventsAsync(string eventName, string[] groupBy, AggregationFunction function, DateTime? startDate, DateTime? endDate, Guid? tenantId, CancellationToken ct = default);
    Task<FunnelAnalysisResultDto> AnalyzeFunnelAsync(string[] steps, DateTime startDate, DateTime endDate, Guid? tenantId, CancellationToken ct = default);
}

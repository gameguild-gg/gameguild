using System.Text.Json;

namespace GameGuild.Analytics;

public class AnalyticsService(
    IAnalyticsEventRepository eventRepository,
    IKpiDefinitionRepository kpiRepository) : IAnalyticsService
{
    public async Task<AnalyticsEventDto> TrackEventAsync(string eventName, string? propertiesJson, Guid? userId, Guid? tenantId, CancellationToken ct = default)
    {
        var evt = new AnalyticsEvent
        {
            EventName = eventName,
            Properties = propertiesJson,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        if (tenantId.HasValue) evt.TenantId = tenantId.Value;

        var saved = await eventRepository.AddAsync(evt, ct);
        return new AnalyticsEventDto(saved.Id, saved.EventName, saved.Properties, saved.UserId, saved.SessionId, saved.Timestamp);
    }

    public async Task TrackEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default)
    {
        await eventRepository.AddRangeAsync(events, ct);
    }

    public async Task<KpiResultDto> CalculateKpiAsync(string kpiName, DateTime startDate, DateTime endDate, Guid? tenantId, CancellationToken ct = default)
    {
        var kpi = await kpiRepository.GetByNameAsync(kpiName, ct)
            ?? throw new ArgumentException($"KPI '{kpiName}' not found.");

        var count = await eventRepository.CountAsync(kpi.EventName ?? kpiName, startDate, endDate, tenantId, ct);
        return new KpiResultDto(kpiName, count, startDate, endDate, DateTime.UtcNow);
    }

    public async Task<List<TimeSeriesDataPointDto>> GetTimeSeriesAsync(string eventName, DateTime startDate, DateTime endDate, TimeSeriesGranularity granularity, Guid? tenantId, CancellationToken ct = default)
    {
        var events = await eventRepository.GetByEventNameAsync(eventName, startDate, endDate, tenantId, ct);

        var grouped = granularity switch
        {
            TimeSeriesGranularity.Hour => events.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0, DateTimeKind.Utc)),
            TimeSeriesGranularity.Day => events.GroupBy(e => e.Timestamp.Date),
            TimeSeriesGranularity.Week => events.GroupBy(e => e.Timestamp.Date.AddDays(-(int)e.Timestamp.DayOfWeek)),
            TimeSeriesGranularity.Month => events.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => events.GroupBy(e => e.Timestamp.Date)
        };

        return grouped
            .Select(g => new TimeSeriesDataPointDto(g.Key, g.Count()))
            .OrderBy(p => p.Timestamp)
            .ToList();
    }

    public async Task<List<AggregationResultDto>> AggregateEventsAsync(string eventName, string[] groupBy, AggregationFunction function, DateTime? startDate, DateTime? endDate, Guid? tenantId, CancellationToken ct = default)
    {
        var events = await eventRepository.GetByEventNameAsync(eventName, startDate, endDate, tenantId, ct);
        // Simple count-based aggregation — advanced aggregations require property parsing
        var results = new List<AggregationResultDto>
        {
            new(new Dictionary<string, string> { ["event"] = eventName }, events.Count)
        };
        return results;
    }

    public async Task<FunnelAnalysisResultDto> AnalyzeFunnelAsync(string[] steps, DateTime startDate, DateTime endDate, Guid? tenantId, CancellationToken ct = default)
    {
        var funnelSteps = new List<FunnelStepDto>();
        var previousCount = 0;

        foreach (var step in steps)
        {
            var count = await eventRepository.CountAsync(step, startDate, endDate, tenantId, ct);
            var dropOff = previousCount > 0 ? 1.0 - ((double)count / previousCount) : 0.0;
            funnelSteps.Add(new FunnelStepDto(step, count, dropOff));
            previousCount = count == 0 ? previousCount : count;
        }

        var totalUsers = funnelSteps.FirstOrDefault()?.UserCount ?? 0;
        return new FunnelAnalysisResultDto(funnelSteps, startDate, endDate, totalUsers);
    }
}

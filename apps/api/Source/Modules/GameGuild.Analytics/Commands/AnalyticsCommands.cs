using GameGuild.CQRS;

namespace GameGuild.Analytics;

// Commands
public record TrackAnalyticsEventCommand(
    string EventName,
    string? PropertiesJson,
    Guid? UserId,
    Guid? TenantId) : ICommand<AnalyticsEventDto>;

// Queries
public record GetTimeSeriesQuery(
    string EventName,
    DateTime StartDate,
    DateTime EndDate,
    TimeSeriesGranularity Granularity,
    Guid? TenantId) : IQuery<List<TimeSeriesDataPointDto>>;

public record CalculateKpiQuery(
    string KpiName,
    DateTime StartDate,
    DateTime EndDate,
    Guid? TenantId) : IQuery<KpiResultDto>;

public record AnalyzeFunnelQuery(
    string[] Steps,
    DateTime StartDate,
    DateTime EndDate,
    Guid? TenantId) : IQuery<FunnelAnalysisResultDto>;

// Handlers
public class TrackAnalyticsEventCommandHandler(IAnalyticsService service) : ICommandHandler<TrackAnalyticsEventCommand, AnalyticsEventDto>
{
    public async Task<AnalyticsEventDto> Handle(TrackAnalyticsEventCommand request, CancellationToken cancellationToken)
        => await service.TrackEventAsync(request.EventName, request.PropertiesJson, request.UserId, request.TenantId, cancellationToken);
}

public class GetTimeSeriesQueryHandler(IAnalyticsService service) : IQueryHandler<GetTimeSeriesQuery, List<TimeSeriesDataPointDto>>
{
    public async Task<List<TimeSeriesDataPointDto>> Handle(GetTimeSeriesQuery request, CancellationToken cancellationToken)
        => await service.GetTimeSeriesAsync(request.EventName, request.StartDate, request.EndDate, request.Granularity, request.TenantId, cancellationToken);
}

public class CalculateKpiQueryHandler(IAnalyticsService service) : IQueryHandler<CalculateKpiQuery, KpiResultDto>
{
    public async Task<KpiResultDto> Handle(CalculateKpiQuery request, CancellationToken cancellationToken)
        => await service.CalculateKpiAsync(request.KpiName, request.StartDate, request.EndDate, request.TenantId, cancellationToken);
}

public class AnalyzeFunnelQueryHandler(IAnalyticsService service) : IQueryHandler<AnalyzeFunnelQuery, FunnelAnalysisResultDto>
{
    public async Task<FunnelAnalysisResultDto> Handle(AnalyzeFunnelQuery request, CancellationToken cancellationToken)
        => await service.AnalyzeFunnelAsync(request.Steps, request.StartDate, request.EndDate, request.TenantId, cancellationToken);
}

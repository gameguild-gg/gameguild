using GameGuild.CQRS;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Commands;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for recording SLI metrics.
/// </summary>
public class RecordSliMetricHandler : IRequestHandler<RecordSliMetricCommand, Result<Unit>>
{
    private readonly ISlaMonitoringService _slaMonitoringService;

    public RecordSliMetricHandler(ISlaMonitoringService slaMonitoringService)
    {
        _slaMonitoringService = slaMonitoringService;
    }

    public async Task<Result<Unit>> Handle(RecordSliMetricCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _slaMonitoringService.RecordSliMetricAsync(
                request.SloId,
                request.MetricValue,
                request.IsSuccessful,
                cancellationToken
            );

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to record SLI metric: {ex.Message}");
        }
    }
}

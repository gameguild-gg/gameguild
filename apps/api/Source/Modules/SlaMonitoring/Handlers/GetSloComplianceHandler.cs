using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Queries;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for getting SLO compliance status.
/// </summary>
public class GetSloComplianceHandler : IRequestHandler<GetSloComplianceQuery, Result<SloComplianceDto>>
{
    private readonly ISlaMonitoringService _slaMonitoringService;

    public GetSloComplianceHandler(ISlaMonitoringService slaMonitoringService)
    {
        _slaMonitoringService = slaMonitoringService;
    }

    public async Task<Result<SloComplianceDto>> Handle(GetSloComplianceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            var compliance = await _slaMonitoringService.GetComplianceStatusAsync(
                request.SloId,
                startDate,
                endDate,
                cancellationToken
            );

            return Result<SloComplianceDto>.Success(compliance);
        }
        catch (Exception ex)
        {
            return Result<SloComplianceDto>.Failure($"Failed to get compliance status: {ex.Message}");
        }
    }
}

using GameGuild.CQRS;
using MediatR;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Queries;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for getting error budget details.
/// </summary>
public class GetErrorBudgetHandler : IRequestHandler<GetErrorBudgetQuery, Result<ErrorBudgetDto>>
{
    private readonly ISlaMonitoringService _slaMonitoringService;

    public GetErrorBudgetHandler(ISlaMonitoringService slaMonitoringService)
    {
        _slaMonitoringService = slaMonitoringService;
    }

    public async Task<Result<ErrorBudgetDto>> Handle(GetErrorBudgetQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var errorBudget = await _slaMonitoringService.CalculateErrorBudgetAsync(
                request.SloId,
                cancellationToken
            );

            return Result<ErrorBudgetDto>.Success(errorBudget);
        }
        catch (Exception ex)
        {
            return Result<ErrorBudgetDto>.Failure($"Failed to get error budget: {ex.Message}");
        }
    }
}

using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Queries;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for getting SLO violations.
/// </summary>
public class GetSloViolationsHandler : IRequestHandler<GetSloViolationsQuery, Result<IEnumerable<SloViolationDto>>>
{
    private readonly ISlaMonitoringService _slaMonitoringService;

    public GetSloViolationsHandler(ISlaMonitoringService slaMonitoringService)
    {
        _slaMonitoringService = slaMonitoringService;
    }

    public async Task<Result<IEnumerable<SloViolationDto>>> Handle(GetSloViolationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<SloViolationDto> violations;

            if (request.OnlyActive)
            {
                violations = await _slaMonitoringService.GetActiveSloViolationsAsync(
                    request.TenantId,
                    cancellationToken
                );
            }
            else
            {
                violations = await _slaMonitoringService.GetActiveSloViolationsAsync(
                    request.TenantId,
                    cancellationToken
                );

                // Filter by SloId if specified
                if (request.SloId.HasValue)
                    violations = violations.Where(v => v.SloId == request.SloId.Value);

                // Filter by date range if specified
                if (request.StartDate.HasValue)
                    violations = violations.Where(v => v.StartedAt >= request.StartDate.Value);

                if (request.EndDate.HasValue)
                    violations = violations.Where(v => v.StartedAt <= request.EndDate.Value);
            }

            // Apply pagination
            var result = violations.Skip(request.Skip).Take(request.Take).ToList();

            return Result<IEnumerable<SloViolationDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SloViolationDto>>.Failure($"Failed to get violations: {ex.Message}");
        }
    }
}

using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

/// <summary>
///     Handler for retrieving SLO compliance status.
/// </summary>
public class GetSloComplianceQueryHandler(ISlaMonitoringService slaMonitoringService, IServiceLevelObjectiveRepository sloRepository) : IQueryHandler<GetSloComplianceQuery, SloComplianceDto>
{
    public async Task<SloComplianceDto> Handle(GetSloComplianceQuery request, CancellationToken cancellationToken)
    {
        var slo = await sloRepository.GetByIdAsync(request.SloId, cancellationToken);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID '{request.SloId}' not found."); }

        if (slo.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to access this SLO."); }

        var startDate = request.StartDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTimeOffset.UtcNow;

        var compliance = await slaMonitoringService.GetComplianceAsync(request.SloId, startDate, endDate, cancellationToken);

        return compliance;
    }
}

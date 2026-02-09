using GameGuild.CQRS;



namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Handler for retrieving SLO compliance status.
/// </summary>
public sealed class GetSloComplianceQueryHandler(ISlaMonitoringService slaMonitoringService, IServiceLevelObjectiveRepository sloRepository) : IQueryHandler<GetSloComplianceQuery, SloComplianceDto>
{
    public async Task<SloComplianceDto> Handle(GetSloComplianceQuery request, CancellationToken cancellationToken)
    {
        var slo = await sloRepository.GetByIdAsync(request.SloId, cancellationToken).ConfigureAwait(false);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID '{request.SloId}' not found."); }

        if (slo.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to access this SLO."); }

        var startDate = request.StartDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTimeOffset.UtcNow;

        var compliance = await slaMonitoringService.GetComplianceAsync(request.SloId, startDate, endDate, cancellationToken).ConfigureAwait(false);

        return compliance;
    }
}

using GameGuild.CQRS;



namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Handler for retrieving SLO violations with filtering.
/// </summary>
public class GetSloViolationsQueryHandler(ISloViolationRepository violationRepository, IServiceLevelObjectiveRepository sloRepository) : IQueryHandler<GetSloViolationsQuery, List<SloViolationDto>>
{
    private readonly IServiceLevelObjectiveRepository _sloRepository = sloRepository;

    public async Task<List<SloViolationDto>> Handle(GetSloViolationsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<SloViolation> violations;

        // Get violations based on filters
        if (request.SloId.HasValue)
        {
            if (request.OnlyUnresolved) { violations = await violationRepository.GetOngoingViolationsAsync(request.SloId.Value, cancellationToken).ConfigureAwait(false); }
            else { violations = await violationRepository.GetBySloIdAsync(request.SloId.Value, cancellationToken).ConfigureAwait(false); }
        }
        else if (request.TenantId.HasValue) { violations = await violationRepository.GetByTenantIdAsync(request.TenantId.Value, cancellationToken).ConfigureAwait(false); }
        else
        {
            // If no filters, get by tenant (tenant context is now extracted by the controller via IActorContextAccessor)
            throw new InvalidOperationException("Either SloId or TenantId must be provided");
        }

        // Apply additional filters
        var filteredViolations = violations.AsEnumerable();

        if (request.OnlyUnresolved) { filteredViolations = filteredViolations.Where(v => !v.EndedAt.HasValue); }

        if (request.StartDate.HasValue) { filteredViolations = filteredViolations.Where(v => v.StartedAt >= request.StartDate.Value); }

        if (request.EndDate.HasValue) { filteredViolations = filteredViolations.Where(v => v.StartedAt <= request.EndDate.Value); }

        // Apply pagination
        var pagedViolations = filteredViolations.Skip(request.Skip).Take(request.Take).ToList();

        // Pre-load SLO names for all violations in a single query
        var sloIds = pagedViolations.Select(v => v.ServiceLevelObjectiveId).Distinct().ToList();
        var sloLookup = new Dictionary<Guid, (string Name, string ServiceName)>();
        foreach (var sloId in sloIds)
        {
            var slo = await _sloRepository.GetByIdAsync(sloId, cancellationToken).ConfigureAwait(false);
            if (slo != null)
                sloLookup[sloId] = (slo.Name, slo.ServiceName);
        }

        // Map to DTOs
        return pagedViolations.Select(v => new SloViolationDto
                {
                    Id = v.Id,
                    ServiceLevelObjectiveId = v.ServiceLevelObjectiveId,
                    SloName = sloLookup.TryGetValue(v.ServiceLevelObjectiveId, out var sloInfo) ? sloInfo.Name : string.Empty,
                    ServiceName = sloLookup.TryGetValue(v.ServiceLevelObjectiveId, out var sloInfo2) ? sloInfo2.ServiceName : string.Empty,
                    StartedAt = v.StartedAt,
                    EndedAt = v.EndedAt,
                    DurationMinutes = v.GetDuration().TotalMinutes,
                    ActualValue = v.ActualValue,
                    TargetValue = v.TargetValue,
                    Severity = v.Severity,
                    AlertTriggered = v.AlertTriggered,
                    AlertSentAt = v.AlertSentAt,
                    IsAcknowledged = v.IsAcknowledged,
                    AcknowledgedByUserId = v.AcknowledgedByUserId,
                    AcknowledgedAt = v.AcknowledgedAt,
                    Notes = v.Notes,
                    Description = v.Description
                }
            )
            .ToList();
    }
}

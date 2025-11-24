using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

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
            if (request.OnlyUnresolved) { violations = await violationRepository.GetOngoingViolationsAsync(request.SloId.Value, cancellationToken); }
            else { violations = await violationRepository.GetBySloIdAsync(request.SloId.Value, cancellationToken); }
        }
        else if (request.TenantId.HasValue) { violations = await violationRepository.GetByTenantIdAsync(request.TenantId.Value, cancellationToken); }
        else
        {
            // If no filters, get by tenant (TODO: should extract from context)
            throw new InvalidOperationException("Either SloId or TenantId must be provided");
        }

        // Apply additional filters
        var filteredViolations = violations.AsEnumerable();

        if (request.OnlyUnresolved) { filteredViolations = filteredViolations.Where(v => !v.EndedAt.HasValue); }

        if (request.StartDate.HasValue) { filteredViolations = filteredViolations.Where(v => v.StartedAt >= request.StartDate.Value); }

        if (request.EndDate.HasValue) { filteredViolations = filteredViolations.Where(v => v.StartedAt <= request.EndDate.Value); }

        // Apply pagination
        var pagedViolations = filteredViolations.Skip(request.Skip).Take(request.Take).ToList();

        // Map to DTOs
        return pagedViolations.Select(v => new SloViolationDto
                {
                    Id = v.Id,
                    ServiceLevelObjectiveId = v.ServiceLevelObjectiveId,
                    SloName = "", // TODO: Load from ServiceLevelObjective navigation property
                    ServiceName = "", // TODO: Load from ServiceLevelObjective navigation property
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

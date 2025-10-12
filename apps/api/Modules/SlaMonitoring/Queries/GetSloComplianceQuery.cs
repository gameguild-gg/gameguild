using GameGuild.CQRS;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Queries;

/// <summary>
/// Query to get compliance status for an SLO.
/// </summary>
public record GetSloComplianceQuery(
    Guid SloId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<Result<SloComplianceDto>>;

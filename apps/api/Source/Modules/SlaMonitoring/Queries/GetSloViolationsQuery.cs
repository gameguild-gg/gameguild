using GameGuild.CQRS;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Queries;

/// <summary>
/// Query to get SLO violations with optional filtering.
/// </summary>
public record GetSloViolationsQuery(
    Guid? SloId = null,
    Guid? TenantId = null,
    bool OnlyActive = false,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Skip = 0,
    int Take = 50
) : IRequest<Result<IEnumerable<SloViolationDto>>>;

using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

/// <summary>
///     Query to retrieve SLO violations with optional filtering.
/// </summary>
public record GetSloViolationsQuery(
    Guid? SloId = null,
    Guid? TenantId = null,
    bool OnlyUnresolved = false,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int Skip = 0,
    int Take = 50
) : IQuery<List<SloViolationDto>>;

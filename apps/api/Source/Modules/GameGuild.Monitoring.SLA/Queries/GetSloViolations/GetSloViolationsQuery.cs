using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Query to retrieve SLO violations with optional filtering.
/// </summary>
public sealed record GetSloViolationsQuery(
    Guid? SloId = null,
    Guid? TenantId = null,
    bool OnlyUnresolved = false,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int Skip = 0,
    int Take = 50
) : IQuery<List<SloViolationDto>>;

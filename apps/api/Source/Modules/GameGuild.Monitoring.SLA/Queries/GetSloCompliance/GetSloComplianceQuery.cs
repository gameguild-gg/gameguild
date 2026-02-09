using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Query to get compliance status for an SLO over a specific time period.
/// </summary>
public sealed record GetSloComplianceQuery(Guid SloId, Guid TenantId, DateTimeOffset? StartDate = null, DateTimeOffset? EndDate = null) : IQuery<SloComplianceDto>;

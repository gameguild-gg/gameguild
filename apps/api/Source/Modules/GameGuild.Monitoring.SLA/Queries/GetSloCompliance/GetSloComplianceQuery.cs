using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

/// <summary>
///     Query to get compliance status for an SLO over a specific time period.
/// </summary>
public record GetSloComplianceQuery(Guid SloId, Guid TenantId, DateTimeOffset? StartDate = null, DateTimeOffset? EndDate = null) : IQuery<SloComplianceDto>;

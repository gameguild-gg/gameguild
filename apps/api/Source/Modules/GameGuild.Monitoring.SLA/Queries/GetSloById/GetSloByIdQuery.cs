using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

public sealed record GetSloByIdQuery(Guid Id, Guid TenantId) : IQuery<SloDto?>;

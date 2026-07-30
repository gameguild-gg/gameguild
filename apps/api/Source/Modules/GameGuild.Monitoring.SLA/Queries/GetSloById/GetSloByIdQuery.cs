using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

public sealed record GetSloByIdQuery(Guid Id, Guid TenantId) : IQuery<SloDto?>;

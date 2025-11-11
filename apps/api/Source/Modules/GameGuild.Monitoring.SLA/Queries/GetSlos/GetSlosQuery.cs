using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

public sealed record GetSlosQuery(Guid TenantId, string? ServiceName = null, bool? IsEnabled = null, int Skip = 0, int Take = 50) : IQuery<List<SloDto>>;

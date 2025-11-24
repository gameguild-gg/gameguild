using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

public sealed record GetErrorBudgetQuery(Guid SloId, Guid TenantId) : IQuery<ErrorBudgetDto?>;

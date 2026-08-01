using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

public sealed record GetErrorBudgetQuery(Guid SloId, Guid TenantId) : IQuery<ErrorBudgetDto?>;

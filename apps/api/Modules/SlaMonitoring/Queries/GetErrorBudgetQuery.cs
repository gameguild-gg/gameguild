using GameGuild.CQRS;
using GameGuild.Modules.SlaMonitoring.Services;

namespace GameGuild.Modules.SlaMonitoring.Queries;

/// <summary>
/// Query to get error budget details for an SLO.
/// </summary>
public record GetErrorBudgetQuery(
    Guid SloId
) : IRequest<Result<ErrorBudgetDto>>;

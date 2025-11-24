using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

public class GetErrorBudgetQueryHandler(IServiceLevelObjectiveRepository sloRepository, IErrorBudgetCalculator errorBudgetCalculator) : IQueryHandler<GetErrorBudgetQuery, ErrorBudgetDto?>
{
    public async Task<ErrorBudgetDto?> Handle(GetErrorBudgetQuery request, CancellationToken cancellationToken)
    {
        var slo = await sloRepository.GetByIdAsync(request.SloId, cancellationToken);

        if (slo == null || slo.TenantId != request.TenantId) { return null; }

        return await errorBudgetCalculator.CalculateAsync(request.SloId, cancellationToken);
    }
}

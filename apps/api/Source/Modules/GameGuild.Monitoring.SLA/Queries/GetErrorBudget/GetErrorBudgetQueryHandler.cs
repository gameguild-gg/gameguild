using GameGuild.CQRS;



namespace GameGuild.Monitoring.SLA;

public sealed class GetErrorBudgetQueryHandler(IServiceLevelObjectiveRepository sloRepository, IErrorBudgetCalculator errorBudgetCalculator) : IQueryHandler<GetErrorBudgetQuery, ErrorBudgetDto?>
{
    public async Task<ErrorBudgetDto?> Handle(GetErrorBudgetQuery request, CancellationToken cancellationToken)
    {
        var slo = await sloRepository.GetByIdAsync(request.SloId, cancellationToken).ConfigureAwait(false);

        if (slo == null || slo.TenantId != request.TenantId) { return null; }

        return await errorBudgetCalculator.CalculateAsync(request.SloId, cancellationToken).ConfigureAwait(false);
    }
}

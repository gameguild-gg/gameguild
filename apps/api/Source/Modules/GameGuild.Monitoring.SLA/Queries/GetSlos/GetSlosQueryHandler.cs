using GameGuild.CQRS;


namespace GameGuild.Monitoring.SLA;

public sealed class GetSlosQueryHandler(IServiceLevelObjectiveRepository repository) : IQueryHandler<GetSlosQuery, List<SloDto>>
{
    public async Task<List<SloDto>> Handle(GetSlosQuery request, CancellationToken cancellationToken)
    {
        var slos = await repository.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        // Apply filters
        var query = slos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.ServiceName)) { query = query.Where(s => s.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase)); }

        if (request.IsEnabled.HasValue) { query = query.Where(s => s.IsEnabled == request.IsEnabled.Value); }

        // Apply pagination
        var results = query.Skip(request.Skip)
            .Take(request.Take)
            .Select(slo => new SloDto
            {
                Id = slo.Id,
                TenantId = slo.TenantId ?? Guid.Empty,
                Name = slo.Name,
                Description = slo.Description,
                ServiceName = slo.ServiceName,
                TargetPercentage = slo.TargetPercentage,
                TimeWindowDays = slo.TimeWindowDays,
                ErrorBudgetPercentage = slo.ErrorBudgetPercentage,
                AlertThresholdPercentage = slo.AlertThresholdPercentage,
                IsEnabled = slo.IsEnabled,
                Status = slo.Status,
                LastEvaluatedAt = slo.LastEvaluatedAt,
                CurrentActualPercentage = slo.CurrentActualPercentage,
                RemainingErrorBudget = slo.RemainingErrorBudget,
                CreatedAt = slo.CreatedAt,
                UpdatedAt = slo.UpdatedAt
            }
            )
            .ToList();

        return results;
    }
}

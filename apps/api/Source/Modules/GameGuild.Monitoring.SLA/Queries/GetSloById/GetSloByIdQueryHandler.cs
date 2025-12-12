using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Queries;

public class GetSloByIdQueryHandler(IServiceLevelObjectiveRepository repository) : IQueryHandler<GetSloByIdQuery, SloDto?>
{
    public async Task<SloDto?> Handle(GetSloByIdQuery request, CancellationToken cancellationToken)
    {
        var slo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (slo == null || slo.TenantId != request.TenantId) { return null; }

        return new SloDto
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
        };
    }
}

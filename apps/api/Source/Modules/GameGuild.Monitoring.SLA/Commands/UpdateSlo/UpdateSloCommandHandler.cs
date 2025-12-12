using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Commands;

public class UpdateSloCommandHandler(IServiceLevelObjectiveRepository repository) : ICommandHandler<UpdateSloCommand, SloDto>
{
    public async Task<SloDto> Handle(UpdateSloCommand request, CancellationToken cancellationToken)
    {
        var slo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID '{request.Id}' not found."); }

        if (slo.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to update this SLO."); }

        // Update properties
        slo.Name = request.Name;
        slo.Description = request.Description;
        slo.ServiceName = request.ServiceName;
        slo.TargetPercentage = request.TargetPercentage;
        slo.TimeWindowDays = request.TimeWindowDays;
        slo.ErrorBudgetPercentage = request.ErrorBudgetPercentage;
        slo.AlertThresholdPercentage = request.AlertThresholdPercentage;

        if (request.IsEnabled && !slo.IsEnabled) { slo.Enable(); }
        else if (!request.IsEnabled && slo.IsEnabled) { slo.Disable(); }

        await repository.UpdateAsync(slo, cancellationToken);

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

using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Enums;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Commands;

public class CreateSloCommandHandler(IServiceLevelObjectiveRepository repository) : ICommandHandler<CreateSloCommand, SloDto>
{
    public async Task<SloDto> Handle(CreateSloCommand request, CancellationToken cancellationToken)
    {
        // Check if SLO with same name exists for this tenant
        var exists = await repository.ExistsByNameAsync(request.Name, request.TenantId, cancellationToken);

        if (exists) { throw new InvalidOperationException($"SLO with name '{request.Name}' already exists for this tenant."); }

        var slo = new ServiceLevelObjective
        {
            Name = request.Name,
            Description = request.Description,
            ServiceName = request.ServiceName,
            TargetPercentage = request.TargetPercentage,
            TimeWindowDays = request.TimeWindowDays,
            ErrorBudgetPercentage = request.ErrorBudgetPercentage,
            AlertThresholdPercentage = request.AlertThresholdPercentage,
            IsEnabled = true,
            Status = SloStatus.Active
        };

        // Set TenantId using SetProperties method
        slo.SetProperties(new Dictionary<string, object?> { { "TenantId", new TenantId(request.TenantId) } });

        await repository.AddAsync(slo, cancellationToken);

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
            CreatedAt = slo.CreatedAt,
            UpdatedAt = slo.UpdatedAt
        };
    }
}

using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Commands;
using GameGuild.Modules.SlaMonitoring.Entities;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for creating service level objectives.
/// </summary>
public class CreateSloHandler : IRequestHandler<CreateSloCommand, Result<Guid>>
{
    private readonly IServiceLevelObjectiveRepository _sloRepository;

    public CreateSloHandler(IServiceLevelObjectiveRepository sloRepository)
    {
        _sloRepository = sloRepository;
    }

    public async Task<Result<Guid>> Handle(CreateSloCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var slo = new ServiceLevelObjective
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Name = request.Name,
                Description = request.Description,
                ServiceName = request.ServiceName,
                TargetPercentage = request.TargetPercentage,
                TimeWindowDays = request.TimeWindowDays,
                ErrorBudgetPercentage = request.ErrorBudgetPercentage ?? (100 - request.TargetPercentage),
                AlertThresholdPercentage = request.AlertThresholdPercentage,
                IsActive = true,
                CurrentStatus = SloStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sloRepository.AddAsync(slo, cancellationToken);

            return Result<Guid>.Success(slo.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Failed to create SLO: {ex.Message}");
        }
    }
}

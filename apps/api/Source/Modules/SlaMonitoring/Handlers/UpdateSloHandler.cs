using GameGuild.CQRS;
using MediatR;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Commands;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for updating service level objectives.
/// </summary>
public class UpdateSloHandler : IRequestHandler<UpdateSloCommand, Result<Unit>>
{
    private readonly IServiceLevelObjectiveRepository _sloRepository;

    public UpdateSloHandler(IServiceLevelObjectiveRepository sloRepository)
    {
        _sloRepository = sloRepository;
    }

    public async Task<Result<Unit>> Handle(UpdateSloCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var slo = await _sloRepository.GetByIdAsync(request.SloId, cancellationToken);
            if (slo == null)
                return Result<Unit>.Failure($"SLO {request.SloId} not found");

            if (request.Name != null)
                slo.Name = request.Name;

            if (request.Description != null)
                slo.Description = request.Description;

            if (request.ServiceName != null)
                slo.ServiceName = request.ServiceName;

            if (request.TargetPercentage.HasValue)
                slo.TargetPercentage = request.TargetPercentage.Value;

            if (request.TimeWindowDays.HasValue)
                slo.TimeWindowDays = request.TimeWindowDays.Value;

            if (request.ErrorBudgetPercentage.HasValue)
                slo.ErrorBudgetPercentage = request.ErrorBudgetPercentage.Value;

            if (request.AlertThresholdPercentage.HasValue)
                slo.AlertThresholdPercentage = request.AlertThresholdPercentage.Value;

            if (request.IsActive.HasValue)
                slo.IsActive = request.IsActive.Value;

            slo.UpdatedAt = DateTime.UtcNow;

            await _sloRepository.UpdateAsync(slo, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to update SLO: {ex.Message}");
        }
    }
}

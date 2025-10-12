using GameGuild.CQRS;
using GameGuild.Modules.SlaMonitoring.Commands;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for deleting service level objectives.
/// </summary>
public class DeleteSloHandler : IRequestHandler<DeleteSloCommand, Result<Unit>>
{
    private readonly IServiceLevelObjectiveRepository _sloRepository;

    public DeleteSloHandler(IServiceLevelObjectiveRepository sloRepository)
    {
        _sloRepository = sloRepository;
    }

    public async Task<Result<Unit>> Handle(DeleteSloCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var slo = await _sloRepository.GetByIdAsync(request.SloId, cancellationToken);
            if (slo == null)
                return Result<Unit>.Failure($"SLO {request.SloId} not found");

            await _sloRepository.DeleteAsync(request.SloId, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to delete SLO: {ex.Message}");
        }
    }
}

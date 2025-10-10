using MediatR;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Queries;
using GameGuild.Modules.SlaMonitoring.Entities;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for getting an SLO by ID.
/// </summary>
public class GetSloByIdHandler : IRequestHandler<GetSloByIdQuery, Result<ServiceLevelObjective>>
{
    private readonly IServiceLevelObjectiveRepository _sloRepository;

    public GetSloByIdHandler(IServiceLevelObjectiveRepository sloRepository)
    {
        _sloRepository = sloRepository;
    }

    public async Task<Result<ServiceLevelObjective>> Handle(GetSloByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var slo = await _sloRepository.GetByIdAsync(request.SloId, cancellationToken);

            if (slo == null)
                return Result<ServiceLevelObjective>.Failure($"SLO {request.SloId} not found");

            return Result<ServiceLevelObjective>.Success(slo);
        }
        catch (Exception ex)
        {
            return Result<ServiceLevelObjective>.Failure($"Failed to get SLO: {ex.Message}");
        }
    }
}

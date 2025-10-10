using GameGuild.CQRS;
using GameGuild.CQRS;
using GameGuild.Core;
using GameGuild.Modules.SlaMonitoring.Queries;
using GameGuild.Modules.SlaMonitoring.Entities;
using GameGuild.Modules.SlaMonitoring.Repositories;

namespace GameGuild.Modules.SlaMonitoring.Handlers;

/// <summary>
/// Handler for getting SLOs with optional filtering.
/// </summary>
public class GetSlosHandler : IRequestHandler<GetSlosQuery, Result<IEnumerable<ServiceLevelObjective>>>
{
    private readonly IServiceLevelObjectiveRepository _sloRepository;

    public GetSlosHandler(IServiceLevelObjectiveRepository sloRepository)
    {
        _sloRepository = sloRepository;
    }

    public async Task<Result<IEnumerable<ServiceLevelObjective>>> Handle(GetSlosQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var slos = await _sloRepository.GetAllAsync(request.TenantId, cancellationToken);

            // Apply filtering
            if (request.IsActive.HasValue)
                slos = slos.Where(s => s.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.ServiceName))
                slos = slos.Where(s => s.ServiceName.Contains(request.ServiceName, StringComparison.OrdinalIgnoreCase));

            // Apply pagination
            var result = slos.Skip(request.Skip).Take(request.Take).ToList();

            return Result<IEnumerable<ServiceLevelObjective>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ServiceLevelObjective>>.Failure($"Failed to get SLOs: {ex.Message}");
        }
    }
}

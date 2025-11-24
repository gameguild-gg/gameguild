using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;

namespace GameGuild.Monitoring.SLA.Commands;

public class DeleteSloCommandHandler(IServiceLevelObjectiveRepository repository) : ICommandHandler<DeleteSloCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSloCommand request, CancellationToken cancellationToken)
    {
        var slo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID '{request.Id}' not found."); }

        if (slo.TenantId != request.TenantId) { throw new UnauthorizedAccessException("You do not have permission to delete this SLO."); }

        await repository.DeleteAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}

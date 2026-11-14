using GameGuild.CQRS;


namespace GameGuild.Monitoring.SLA;

public sealed class DeleteSloCommandHandler(IServiceLevelObjectiveRepository repository) : ICommandHandler<DeleteSloCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSloCommand request, CancellationToken cancellationToken)
    {
        var slo = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (slo == null) { throw new EntityNotFoundException("ServiceLevelObjective", request.Id); }

        if (slo.TenantId != request.TenantId) { throw new AccessDeniedException("You do not have permission to delete this SLO."); }

        await repository.DeleteAsync(request.Id, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

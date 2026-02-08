using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for ResolveDisputeCommand
/// </summary>
public sealed class ResolveDisputeCommandHandler(IDisputeService disputeService) : ICommandHandler<ResolveDisputeCommand>
{
    public async Task<Unit> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        await disputeService.ResolveDisputeAsync(request.DisputeId, request.Resolution, request.Notes, request.ResolvedBy, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

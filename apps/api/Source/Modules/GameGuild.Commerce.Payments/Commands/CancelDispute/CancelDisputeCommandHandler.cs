using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CancelDisputeCommand
/// </summary>
public sealed class CancelDisputeCommandHandler(IDisputeService disputeService) : ICommandHandler<CancelDisputeCommand>
{
    public async Task<Unit> Handle(CancelDisputeCommand request, CancellationToken cancellationToken)
    {
        await disputeService.CancelDisputeAsync(request.DisputeId, request.Reason, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

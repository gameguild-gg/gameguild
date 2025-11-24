using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Handler for CancelDisputeCommand
/// </summary>
public sealed class CancelDisputeCommandHandler(IDisputeService disputeService) : ICommandHandler<CancelDisputeCommand>
{
    public async Task<Unit> Handle(CancelDisputeCommand request, CancellationToken cancellationToken)
    {
        await disputeService.CancelDisputeAsync(request.DisputeId, request.Reason, cancellationToken);

        return Unit.Value;
    }
}

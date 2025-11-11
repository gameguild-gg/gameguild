using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Handler for UpdateDisputeStatusCommand
/// </summary>
public sealed class UpdateDisputeStatusCommandHandler(IDisputeService disputeService) : ICommandHandler<UpdateDisputeStatusCommand>
{
    public async Task<Unit> Handle(UpdateDisputeStatusCommand request, CancellationToken cancellationToken)
    {
        await disputeService.UpdateDisputeStatusAsync(request.DisputeId, request.NewStatus, request.DueDate, cancellationToken);

        return Unit.Value;
    }
}

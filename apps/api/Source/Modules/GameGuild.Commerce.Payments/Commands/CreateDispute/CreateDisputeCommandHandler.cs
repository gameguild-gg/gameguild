using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateDisputeCommand
/// </summary>
public sealed class CreateDisputeCommandHandler(IDisputeService disputeService) : ICommandHandler<CreateDisputeCommand, PaymentDispute>
{
    public async Task<PaymentDispute> Handle(CreateDisputeCommand request, CancellationToken cancellationToken)
    {
        return await disputeService.CreateDisputeAsync(request.PaymentId, request.UserId, request.Type, request.Amount, request.Reason, request.Description, cancellationToken);
    }
}

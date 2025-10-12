using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageDisputes;

/// <summary>Handler for CreateDisputeCommand</summary>
public class CreateDisputeHandler : IRequestHandler<CreateDisputeCommand, PaymentDispute>
{
    private readonly IDisputeService _disputeService;

    public CreateDisputeHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<PaymentDispute> Handle(CreateDisputeCommand request, CancellationToken cancellationToken)
    {
        return await _disputeService.CreateDisputeAsync(
            request.PaymentId,
            request.UserId,
            request.Type,
            request.Amount,
            request.Reason,
            request.Description,
            cancellationToken);
    }
}

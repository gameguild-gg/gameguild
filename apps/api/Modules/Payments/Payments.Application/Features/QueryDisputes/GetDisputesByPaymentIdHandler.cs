using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Handler for GetDisputesByPaymentIdQuery</summary>
public class GetDisputesByPaymentIdHandler : IRequestHandler<GetDisputesByPaymentIdQuery, List<PaymentDispute>>
{
    private readonly IDisputeService _disputeService;

    public GetDisputesByPaymentIdHandler(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    public async Task<List<PaymentDispute>> Handle(GetDisputesByPaymentIdQuery request, CancellationToken cancellationToken)
    {
        return await _disputeService.GetDisputesByPaymentIdAsync(request.PaymentId, cancellationToken);
    }
}

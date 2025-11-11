using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetDisputesByPaymentIdQuery
/// </summary>
public sealed class GetDisputesByPaymentIdQueryHandler(IDisputeService disputeService) : IQueryHandler<GetDisputesByPaymentIdQuery, List<PaymentDispute>>
{
    public async Task<List<PaymentDispute>> Handle(GetDisputesByPaymentIdQuery request, CancellationToken cancellationToken)
    {
        return await disputeService.GetDisputesByPaymentIdAsync(request.PaymentId, cancellationToken);
    }
}

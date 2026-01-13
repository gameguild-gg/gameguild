using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetDisputesByUserIdQuery
/// </summary>
public sealed class GetDisputesByUserIdQueryHandler(IDisputeService disputeService) : IQueryHandler<GetDisputesByUserIdQuery, List<PaymentDispute>>
{
    public async Task<List<PaymentDispute>> Handle(GetDisputesByUserIdQuery request, CancellationToken cancellationToken)
    {
        return await disputeService.GetDisputesByUserIdAsync(request.UserId, request.Skip, request.Take, cancellationToken);
    }
}

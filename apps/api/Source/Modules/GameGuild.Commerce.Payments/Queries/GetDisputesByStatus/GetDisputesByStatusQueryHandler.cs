using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetDisputesByStatusQuery
/// </summary>
public sealed class GetDisputesByStatusQueryHandler(IDisputeService disputeService) : IQueryHandler<GetDisputesByStatusQuery, List<PaymentDispute>>
{
    public async Task<List<PaymentDispute>> Handle(GetDisputesByStatusQuery request, CancellationToken cancellationToken)
    {
        return await disputeService.GetDisputesByStatusAsync(request.Status, request.Skip, request.Take, cancellationToken);
    }
}

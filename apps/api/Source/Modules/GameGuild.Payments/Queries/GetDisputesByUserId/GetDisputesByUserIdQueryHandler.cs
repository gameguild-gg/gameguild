using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

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

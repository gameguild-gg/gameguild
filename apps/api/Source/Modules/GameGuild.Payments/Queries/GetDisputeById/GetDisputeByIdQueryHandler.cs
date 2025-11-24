using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetDisputeByIdQuery
/// </summary>
public sealed class GetDisputeByIdQueryHandler(IDisputeService disputeService) : IQueryHandler<GetDisputeByIdQuery, PaymentDispute?>
{
    public async Task<PaymentDispute?> Handle(GetDisputeByIdQuery request, CancellationToken cancellationToken) { return await disputeService.GetDisputeByIdAsync(request.DisputeId, cancellationToken); }
}

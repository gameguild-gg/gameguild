using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetRevenueEventsByReferenceIdQuery
/// </summary>
public sealed class GetRevenueEventsByReferenceIdQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetRevenueEventsByReferenceIdQuery, List<RevenueEvent>>
{
    public async Task<List<RevenueEvent>> Handle(GetRevenueEventsByReferenceIdQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetRevenueEventsByReferenceIdAsync(request.ReferenceId, cancellationToken).ConfigureAwait(false);
    }
}

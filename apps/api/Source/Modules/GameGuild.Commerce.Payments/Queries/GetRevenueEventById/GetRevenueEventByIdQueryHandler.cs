using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetRevenueEventByIdQuery
/// </summary>
public sealed class GetRevenueEventByIdQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetRevenueEventByIdQuery, RevenueEvent?>
{
    public async Task<RevenueEvent?> Handle(GetRevenueEventByIdQuery request, CancellationToken cancellationToken) { return await revenueAuditService.GetRevenueEventByIdAsync(request.EventId, cancellationToken).ConfigureAwait(false); }
}

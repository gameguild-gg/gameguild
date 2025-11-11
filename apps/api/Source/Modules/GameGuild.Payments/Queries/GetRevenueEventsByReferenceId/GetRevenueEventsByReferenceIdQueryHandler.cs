using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetRevenueEventsByReferenceIdQuery
/// </summary>
public sealed class GetRevenueEventsByReferenceIdQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetRevenueEventsByReferenceIdQuery, List<RevenueEvent>>
{
    public async Task<List<RevenueEvent>> Handle(GetRevenueEventsByReferenceIdQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetRevenueEventsByReferenceIdAsync(request.ReferenceId, cancellationToken);
    }
}

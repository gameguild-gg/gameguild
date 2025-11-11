using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetRevenueEventsByDateRangeQuery
/// </summary>
public sealed class GetRevenueEventsByDateRangeQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetRevenueEventsByDateRangeQuery, List<RevenueEvent>>
{
    public async Task<List<RevenueEvent>> Handle(GetRevenueEventsByDateRangeQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetRevenueEventsByDateRangeAsync(request.StartDate, request.EndDate, request.Skip, request.Take, cancellationToken);
    }
}

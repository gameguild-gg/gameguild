using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Queries;

public class GetRevenueEventsByDateRangeQueryHandler : IRequestHandler<GetRevenueEventsByDateRangeQuery, List<RevenueEvent>>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public GetRevenueEventsByDateRangeQueryHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<List<RevenueEvent>> Handle(GetRevenueEventsByDateRangeQuery request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.GetRevenueEventsByDateRangeAsync(
            request.StartDate,
            request.EndDate,
            request.Skip,
            request.Take,
            cancellationToken);
    }
}

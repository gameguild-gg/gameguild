using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetRevenueEventByIdQuery
/// </summary>
public sealed class GetRevenueEventByIdQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetRevenueEventByIdQuery, RevenueEvent?>
{
    public async Task<RevenueEvent?> Handle(GetRevenueEventByIdQuery request, CancellationToken cancellationToken) { return await revenueAuditService.GetRevenueEventByIdAsync(request.EventId, cancellationToken); }
}

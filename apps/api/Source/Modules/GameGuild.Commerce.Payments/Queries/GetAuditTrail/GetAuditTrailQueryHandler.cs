using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetAuditTrailQuery
/// </summary>
public sealed class GetAuditTrailQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetAuditTrailQuery, List<AuditTrail>>
{
    public async Task<List<AuditTrail>> Handle(GetAuditTrailQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetAuditTrailByEntityAsync(request.EntityType, request.EntityId, request.Skip, request.Take, cancellationToken);
    }
}

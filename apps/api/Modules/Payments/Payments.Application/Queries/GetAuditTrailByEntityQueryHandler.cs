using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Queries;

public class GetAuditTrailByEntityQueryHandler : IRequestHandler<GetAuditTrailByEntityQuery, List<AuditTrail>>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public GetAuditTrailByEntityQueryHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<List<AuditTrail>> Handle(GetAuditTrailByEntityQuery request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.GetAuditTrailByEntityAsync(
            request.EntityType,
            request.EntityId,
            request.Skip,
            request.Take,
            cancellationToken);
    }
}

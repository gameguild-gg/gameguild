using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public class RecordAuditTrailCommandHandler : IRequestHandler<RecordAuditTrailCommand, AuditTrail>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public RecordAuditTrailCommandHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<AuditTrail> Handle(RecordAuditTrailCommand request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.RecordAuditTrailAsync(
            request.EntityType,
            request.EntityId,
            request.Action,
            request.ChangedBy,
            request.OldValue,
            request.NewValue,
            request.IpAddress,
            request.UserAgent,
            request.Reason,
            request.TenantId,
            cancellationToken);
    }
}

using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public class RecordRevenueEventCommandHandler : IRequestHandler<RecordRevenueEventCommand, RevenueEvent>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public RecordRevenueEventCommandHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<RevenueEvent> Handle(RecordRevenueEventCommand request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.RecordRevenueEventAsync(
            request.EventType,
            request.Amount,
            request.Currency,
            request.Source,
            request.ReferenceId,
            request.UserId,
            request.TenantId,
            request.Metadata,
            cancellationToken);
    }
}

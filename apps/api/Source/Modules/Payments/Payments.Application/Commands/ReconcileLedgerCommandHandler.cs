using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;
using MediatR;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public class ReconcileLedgerCommandHandler : IRequestHandler<ReconcileLedgerCommand, Unit>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public ReconcileLedgerCommandHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<Unit> Handle(ReconcileLedgerCommand request, CancellationToken cancellationToken)
    {
        await _revenueAuditService.ReconcileLedgerEntryAsync(
            request.EntryId,
            request.ReconciledBy,
            request.Notes,
            cancellationToken);

        return Unit.Value;
    }
}

using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for ReconcileLedgerCommand
/// </summary>
public sealed class ReconcileLedgerCommandHandler(IRevenueAuditService revenueAuditService) : ICommandHandler<ReconcileLedgerCommand>
{
    public async Task<Unit> Handle(ReconcileLedgerCommand request, CancellationToken cancellationToken)
    {
        await revenueAuditService.ReconcileLedgerEntryAsync(request.EntryId, request.ReconciledBy, request.Notes, cancellationToken);

        return Unit.Value;
    }
}

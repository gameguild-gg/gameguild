using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateLedgerEntryCommand
/// </summary>
public sealed class CreateLedgerEntryCommandHandler(IRevenueAuditService revenueAuditService) : ICommandHandler<CreateLedgerEntryCommand, FinancialLedgerEntry>
{
    public async Task<FinancialLedgerEntry> Handle(CreateLedgerEntryCommand request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.CreateLedgerEntryAsync(
            request.EntryType,
            request.DebitAccount,
            request.CreditAccount,
            request.Amount,
            request.Currency,
            request.Description,
            request.RevenueEventId,
            request.ReferenceNumber,
            cancellationToken
        ).ConfigureAwait(false);
    }
}

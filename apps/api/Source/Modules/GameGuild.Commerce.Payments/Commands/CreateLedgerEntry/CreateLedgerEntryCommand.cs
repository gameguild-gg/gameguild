using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to create a financial ledger entry
/// </summary>
public record CreateLedgerEntryCommand(
    LedgerEntryType EntryType,
    string DebitAccount,
    string CreditAccount,
    decimal Amount,
    string Currency,
    string Description,
    Guid? RevenueEventId = null,
    string? ReferenceNumber = null
) : ICommand<FinancialLedgerEntry>;

using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public record CreateLedgerEntryCommand(
    LedgerEntryType EntryType,
    string DebitAccount,
    string CreditAccount,
    decimal Amount,
    string Currency,
    string Description,
    Guid? RevenueEventId = null,
    string? ReferenceNumber = null
) : IRequest<FinancialLedgerEntry>;

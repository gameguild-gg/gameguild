using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get unreconciled ledger entries
/// </summary>
public sealed record GetUnreconciledLedgerEntriesQuery(int Skip = 0, int Take = 50) : IQuery<List<FinancialLedgerEntry>>;

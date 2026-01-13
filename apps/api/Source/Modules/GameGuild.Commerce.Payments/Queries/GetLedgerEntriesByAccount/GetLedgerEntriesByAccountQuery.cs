using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get ledger entries by account
/// </summary>
public record GetLedgerEntriesByAccountQuery(string Account, int Skip = 0, int Take = 50) : IQuery<List<FinancialLedgerEntry>>;

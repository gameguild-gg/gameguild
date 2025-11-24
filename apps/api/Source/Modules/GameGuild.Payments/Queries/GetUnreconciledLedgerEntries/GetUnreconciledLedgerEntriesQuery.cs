using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get unreconciled ledger entries
/// </summary>
public record GetUnreconciledLedgerEntriesQuery(int Skip = 0, int Take = 50) : IQuery<List<FinancialLedgerEntry>>;

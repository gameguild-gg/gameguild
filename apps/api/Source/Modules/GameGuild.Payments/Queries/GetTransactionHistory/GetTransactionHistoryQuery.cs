using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get wallet transaction history
/// </summary>
public sealed record GetTransactionHistoryQuery(Guid UserId, int Skip = 0, int Take = 50, WalletTransactionType? TypeFilter = null, TransactionStatus? StatusFilter = null) : IQuery<List<WalletTransaction>>;

using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryWallet;

/// <summary>Query to get wallet transaction history</summary>
public record GetTransactionHistoryQuery(
    Guid UserId,
    int Skip = 0,
    int Take = 50,
    WalletTransactionType? TypeFilter = null,
    TransactionStatus? StatusFilter = null
) : IRequest<List<WalletTransaction>>;

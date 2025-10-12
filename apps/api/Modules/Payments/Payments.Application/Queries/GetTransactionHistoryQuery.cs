using GameGuild.CQRS;
using GameGuild.Modules.Payments.Domain.Entities;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Get transaction history for a user's wallet
/// </summary>
public record GetTransactionHistoryQuery : IRequest<List<WalletTransaction>>
{
    public required Guid UserId { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
    public WalletTransactionType? TypeFilter { get; init; }
    public TransactionStatus? StatusFilter { get; init; }
}

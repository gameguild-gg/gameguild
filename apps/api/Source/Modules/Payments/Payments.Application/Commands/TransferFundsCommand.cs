using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Transfer funds from one user's wallet to another
/// </summary>
public record TransferFundsCommand : IRequest<(WalletTransaction DebitTransaction, WalletTransaction CreditTransaction)>
{
    public required Guid FromUserId { get; init; }
    public required Guid ToUserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? ReferenceId { get; init; }
}

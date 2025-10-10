using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Deduct funds from a user's wallet
/// </summary>
public record DeductFundsCommand : IRequest<WalletTransaction>
{
    public required Guid UserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? ReferenceId { get; init; }
}

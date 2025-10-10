using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Create a new wallet for a user
/// </summary>
public record CreateWalletCommand : IRequest<UserWallet>
{
    public required Guid UserId { get; init; }
    public string Currency { get; init; } = "USD";
}

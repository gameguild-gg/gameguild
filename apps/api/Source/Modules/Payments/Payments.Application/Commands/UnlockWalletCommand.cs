using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Unlock a user's wallet
/// </summary>
public record UnlockWalletCommand : ICommand<Unit>
{
    public required Guid UserId { get; init; }
}

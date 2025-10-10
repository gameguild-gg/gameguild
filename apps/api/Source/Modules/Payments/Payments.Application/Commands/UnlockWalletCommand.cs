using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Unlock a user's wallet
/// </summary>
public record UnlockWalletCommand : IRequest
{
    public required Guid UserId { get; init; }
}

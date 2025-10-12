using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Lock a user's wallet
/// </summary>
public record LockWalletCommand : ICommand<Unit>
{
    public required Guid UserId { get; init; }
    public required string Reason { get; init; }
}

using GameGuild.CQRS;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to lock a user's wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Reason">Reason for locking the wallet</param>
public record LockWalletCommand(Guid UserId, string Reason) : ICommand;

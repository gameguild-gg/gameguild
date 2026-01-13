using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to unlock a user's wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
public record UnlockWalletCommand(Guid UserId) : ICommand;

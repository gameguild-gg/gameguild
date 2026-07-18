using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to unlock a user's wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record UnlockWalletCommand(Guid UserId) : ICommand;

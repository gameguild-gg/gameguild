using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to lock a user's wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Reason">Reason for locking the wallet</param>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record LockWalletCommand(Guid UserId, string Reason) : ICommand;

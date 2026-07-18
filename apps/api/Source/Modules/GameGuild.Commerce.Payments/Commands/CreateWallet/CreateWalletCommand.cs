using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Resources;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to create a new user wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Currency">Currency code (default: USD)</param>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
[RequiresQuota(ResourceUsageType.Wallets, 1, Source = "CreateWallet")]
public sealed record CreateWalletCommand(Guid UserId, string Currency = "USD") : ICommand<UserWallet>;

using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get wallet by user ID
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record GetWalletByUserIdQuery(Guid UserId) : IQuery<UserWallet?>;

using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Queries.GetWalletById;

/// <summary>
///     Query to get a wallet by its ID
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record GetWalletByIdQuery(Guid WalletId) : IQuery<UserWallet?>;

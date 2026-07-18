using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get wallet balance by user ID
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record GetWalletBalanceQuery(Guid UserId) : IQuery<decimal>;

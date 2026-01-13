using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get wallet by user ID
/// </summary>
public sealed record GetWalletByUserIdQuery(Guid UserId) : IQuery<UserWallet?>;

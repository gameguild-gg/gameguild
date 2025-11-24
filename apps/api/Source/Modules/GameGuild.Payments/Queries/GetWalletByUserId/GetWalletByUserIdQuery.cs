using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get wallet by user ID
/// </summary>
public sealed record GetWalletByUserIdQuery(Guid UserId) : IQuery<UserWallet?>;

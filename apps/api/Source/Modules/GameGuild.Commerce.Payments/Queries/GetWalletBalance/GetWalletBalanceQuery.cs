using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get wallet balance by user ID
/// </summary>
public sealed record GetWalletBalanceQuery(Guid UserId) : IQuery<decimal>;

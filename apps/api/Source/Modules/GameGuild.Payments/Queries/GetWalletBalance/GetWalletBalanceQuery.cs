using GameGuild.CQRS;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get wallet balance by user ID
/// </summary>
public sealed record GetWalletBalanceQuery(Guid UserId) : IQuery<decimal>;

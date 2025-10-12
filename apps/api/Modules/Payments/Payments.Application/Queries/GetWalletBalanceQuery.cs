using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Get a wallet balance by user ID
/// </summary>
public record GetWalletBalanceQuery : IRequest<decimal>
{
    public required Guid UserId { get; init; }
}

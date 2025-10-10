using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Get a wallet by user ID
/// </summary>
public record GetWalletQuery : IRequest<UserWallet?>
{
    public required Guid UserId { get; init; }
}

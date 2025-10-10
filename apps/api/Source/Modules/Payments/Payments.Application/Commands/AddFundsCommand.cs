using GameGuild.CQRS;
using GameGuild.Modules.Payments.Entities;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Add funds to a user's wallet
/// </summary>
public record AddFundsCommand : IRequest<WalletTransaction>
{
    public required Guid UserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? ReferenceId { get; init; }
}

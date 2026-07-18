using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to add funds to a user's wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Amount">Amount to add</param>
/// <param name="Description">Transaction description</param>
/// <param name="ReferenceId">Optional reference ID (e.g., order ID)</param>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record AddFundsCommand(Guid UserId, decimal Amount, string Description, string? ReferenceId = null) : ICommand<WalletTransaction>;

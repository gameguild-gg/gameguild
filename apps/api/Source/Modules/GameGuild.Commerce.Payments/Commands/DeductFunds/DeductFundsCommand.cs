using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to deduct funds from a user's wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Amount">Amount to deduct</param>
/// <param name="Description">Transaction description</param>
/// <param name="ReferenceId">Optional reference ID (e.g., order ID)</param>
public sealed record DeductFundsCommand(Guid UserId, decimal Amount, string Description, string? ReferenceId = null) : ICommand<WalletTransaction>;

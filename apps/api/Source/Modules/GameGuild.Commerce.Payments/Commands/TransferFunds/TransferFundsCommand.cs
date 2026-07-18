using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to transfer funds between user wallets
/// </summary>
/// <param name="FromUserId">Source user ID</param>
/// <param name="ToUserId">Destination user ID</param>
/// <param name="Amount">Amount to transfer</param>
/// <param name="Description">Transfer description</param>
/// <param name="ReferenceId">Optional reference ID</param>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record TransferFundsCommand(Guid FromUserId, Guid ToUserId, decimal Amount, string Description, string? ReferenceId = null) : ICommand<TransferResult>;

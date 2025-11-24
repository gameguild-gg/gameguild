using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to create a new user wallet
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Currency">Currency code (default: USD)</param>
public record CreateWalletCommand(Guid UserId, string Currency = "USD") : ICommand<UserWallet>;

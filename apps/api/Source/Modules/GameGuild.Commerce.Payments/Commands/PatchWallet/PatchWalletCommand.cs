using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Commands.PatchWallet;

/// <summary>
///     Command to partially update wallet settings
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record PatchWalletCommand(
    Guid WalletId,
    string? Currency = null,
    decimal? DailyLimit = null,
    decimal? MonthlyLimit = null) : ICommand;

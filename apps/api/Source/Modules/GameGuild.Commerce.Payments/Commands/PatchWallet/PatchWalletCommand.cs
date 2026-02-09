using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.PatchWallet;

/// <summary>
///     Command to partially update wallet settings
/// </summary>
public sealed record PatchWalletCommand(
    Guid WalletId,
    string? Currency = null,
    decimal? DailyLimit = null,
    decimal? MonthlyLimit = null) : ICommand;

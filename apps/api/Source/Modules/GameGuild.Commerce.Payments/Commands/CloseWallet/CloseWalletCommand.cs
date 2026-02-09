using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.CloseWallet;

/// <summary>
///     Command to close/delete a wallet
/// </summary>
public sealed record CloseWalletCommand(Guid WalletId) : ICommand;

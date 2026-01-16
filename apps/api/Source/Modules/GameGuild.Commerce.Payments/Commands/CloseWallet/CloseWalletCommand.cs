using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.CloseWallet;

/// <summary>
///     Command to close/delete a wallet
/// </summary>
public record CloseWalletCommand(Guid WalletId) : ICommand;

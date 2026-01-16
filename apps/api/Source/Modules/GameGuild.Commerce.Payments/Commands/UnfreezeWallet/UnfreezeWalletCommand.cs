using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.UnfreezeWallet;

/// <summary>
///     Command to unfreeze a wallet by ID
/// </summary>
public record UnfreezeWalletCommand(Guid WalletId) : ICommand;

using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.FreezeWallet;

/// <summary>
///     Command to freeze a wallet by ID
/// </summary>
public record FreezeWalletCommand(Guid WalletId, string Reason) : ICommand;

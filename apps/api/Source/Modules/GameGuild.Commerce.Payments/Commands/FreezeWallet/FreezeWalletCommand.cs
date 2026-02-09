using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.FreezeWallet;

/// <summary>
///     Command to freeze a wallet by ID
/// </summary>
public sealed record FreezeWalletCommand(Guid WalletId, string Reason) : ICommand;

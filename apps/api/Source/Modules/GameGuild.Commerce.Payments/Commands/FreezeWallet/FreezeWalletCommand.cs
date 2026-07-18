using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Commands.FreezeWallet;

/// <summary>
///     Command to freeze a wallet by ID
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record FreezeWalletCommand(Guid WalletId, string Reason) : ICommand;

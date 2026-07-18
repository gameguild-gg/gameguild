using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Commands.CloseWallet;

/// <summary>
///     Command to close/delete a wallet
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record CloseWalletCommand(Guid WalletId) : ICommand;

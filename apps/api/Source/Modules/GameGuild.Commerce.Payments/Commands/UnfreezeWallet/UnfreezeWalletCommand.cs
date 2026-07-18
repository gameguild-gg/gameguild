using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Payments.Commands.UnfreezeWallet;

/// <summary>
///     Command to unfreeze a wallet by ID
/// </summary>
[AuthorizeRequest(WalletsPermission.Keys.Admin)]
public sealed record UnfreezeWalletCommand(Guid WalletId) : ICommand;

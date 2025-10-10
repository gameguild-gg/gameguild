using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Command to unlock a wallet</summary>
public record UnlockWalletCommand(
    Guid UserId
) : IRequest;

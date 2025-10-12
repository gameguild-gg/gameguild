using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Command to lock a wallet</summary>
public record LockWalletCommand(
    Guid UserId,
    string Reason
) : IRequest;

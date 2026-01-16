using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.FreezeWallet;

/// <summary>
///     Handler for FreezeWalletCommand
/// </summary>
public sealed class FreezeWalletHandler(IWalletService walletService) : ICommandHandler<FreezeWalletCommand>
{
    public async Task Handle(FreezeWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.FreezeWalletAsync(request.WalletId, request.Reason, cancellationToken);
    }
}

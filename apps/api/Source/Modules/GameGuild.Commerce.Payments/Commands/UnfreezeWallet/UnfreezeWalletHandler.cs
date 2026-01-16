using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.UnfreezeWallet;

/// <summary>
///     Handler for UnfreezeWalletCommand
/// </summary>
public sealed class UnfreezeWalletHandler(IWalletService walletService) : ICommandHandler<UnfreezeWalletCommand>
{
    public async Task<Unit> Handle(UnfreezeWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UnfreezeWalletAsync(request.WalletId, cancellationToken);
        return Unit.Value;
    }
}

using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.CloseWallet;

/// <summary>
///     Handler for CloseWalletCommand
/// </summary>
public sealed class CloseWalletHandler(IWalletService walletService) : ICommandHandler<CloseWalletCommand>
{
    public async Task<Unit> Handle(CloseWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.CloseWalletAsync(request.WalletId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

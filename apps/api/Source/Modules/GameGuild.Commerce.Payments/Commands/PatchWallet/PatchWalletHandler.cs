using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Commands.PatchWallet;

/// <summary>
///     Handler for PatchWalletCommand
/// </summary>
public sealed class PatchWalletHandler(IWalletService walletService) : ICommandHandler<PatchWalletCommand>
{
    public async Task<Unit> Handle(PatchWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UpdateWalletSettingsAsync(
            request.WalletId,
            request.Currency,
            request.DailyLimit,
            request.MonthlyLimit,
            cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

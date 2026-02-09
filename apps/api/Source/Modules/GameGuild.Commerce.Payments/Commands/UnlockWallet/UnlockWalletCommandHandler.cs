using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for UnlockWalletCommand
/// </summary>
public sealed class UnlockWalletCommandHandler(IWalletService walletService) : ICommandHandler<UnlockWalletCommand>
{
    public async Task<Unit> Handle(UnlockWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UnlockWalletAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

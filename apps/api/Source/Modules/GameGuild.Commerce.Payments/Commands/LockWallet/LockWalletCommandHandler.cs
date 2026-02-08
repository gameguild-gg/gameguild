using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for LockWalletCommand
/// </summary>
public class LockWalletCommandHandler(IWalletService walletService) : ICommandHandler<LockWalletCommand>
{
    public async Task<Unit> Handle(LockWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.LockWalletAsync(request.UserId, request.Reason, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

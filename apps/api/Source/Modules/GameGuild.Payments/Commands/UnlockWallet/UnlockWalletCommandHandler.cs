using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Handler for UnlockWalletCommand
/// </summary>
public class UnlockWalletCommandHandler(IWalletService walletService) : ICommandHandler<UnlockWalletCommand>
{
    public async Task<Unit> Handle(UnlockWalletCommand request, CancellationToken cancellationToken)
    {
        await walletService.UnlockWalletAsync(request.UserId, cancellationToken);

        return Unit.Value;
    }
}

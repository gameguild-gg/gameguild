using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for UnlockWalletCommand</summary>
public class UnlockWalletHandler : IRequestHandler<UnlockWalletCommand>
{
    private readonly IWalletService _walletService;

    public UnlockWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task Handle(UnlockWalletCommand request, CancellationToken cancellationToken)
    {
        await _walletService.UnlockWalletAsync(request.UserId, cancellationToken);
    }
}

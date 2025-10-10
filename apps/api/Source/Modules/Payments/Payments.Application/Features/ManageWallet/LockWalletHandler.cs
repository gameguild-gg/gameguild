using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for LockWalletCommand</summary>
public class LockWalletHandler : IRequestHandler<LockWalletCommand>
{
    private readonly IWalletService _walletService;

    public LockWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task Handle(LockWalletCommand request, CancellationToken cancellationToken)
    {
        await _walletService.LockWalletAsync(request.UserId, request.Reason, cancellationToken);
    }
}

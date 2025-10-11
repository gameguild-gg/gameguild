using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;
using MediatR;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for UnlockWalletCommand</summary>
public class UnlockWalletHandler : IRequestHandler<UnlockWalletCommand, Unit>
{
    private readonly IWalletService _walletService;

    public UnlockWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<Unit> Handle(UnlockWalletCommand request, CancellationToken cancellationToken)
    {
        await _walletService.UnlockWalletAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}

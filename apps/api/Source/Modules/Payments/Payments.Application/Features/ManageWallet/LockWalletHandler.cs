using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;
using MediatR;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for LockWalletCommand</summary>
public class LockWalletHandler : IRequestHandler<LockWalletCommand, Unit>
{
    private readonly IWalletService _walletService;

    public LockWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<Unit> Handle(LockWalletCommand request, CancellationToken cancellationToken)
    {
        await _walletService.LockWalletAsync(request.UserId, request.Reason, cancellationToken);
        return Unit.Value;
    }
}

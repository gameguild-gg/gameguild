using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for DeductFundsCommand</summary>
public class DeductFundsHandler : IRequestHandler<DeductFundsCommand, WalletTransaction>
{
    private readonly IWalletService _walletService;

    public DeductFundsHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<WalletTransaction> Handle(DeductFundsCommand request, CancellationToken cancellationToken)
    {
        return await _walletService.DeductFundsAsync(
            request.UserId,
            request.Amount,
            request.Description,
            request.ReferenceId,
            cancellationToken);
    }
}

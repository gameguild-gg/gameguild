using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for TransferFundsCommand</summary>
public class TransferFundsHandler : IRequestHandler<TransferFundsCommand, TransferResult>
{
    private readonly IWalletService _walletService;

    public TransferFundsHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<TransferResult> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        var (debit, credit) = await _walletService.TransferFundsAsync(
            request.FromUserId,
            request.ToUserId,
            request.Amount,
            request.Description,
            request.ReferenceId,
            cancellationToken);

        return new TransferResult(debit, credit);
    }
}

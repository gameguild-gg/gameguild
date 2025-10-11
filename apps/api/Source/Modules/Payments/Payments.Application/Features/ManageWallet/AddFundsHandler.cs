using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for AddFundsCommand</summary>
public class AddFundsHandler : IRequestHandler<AddFundsCommand, WalletTransaction>
{
    private readonly IWalletService _walletService;

    public AddFundsHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<WalletTransaction> Handle(AddFundsCommand request, CancellationToken cancellationToken)
    {
        return await _walletService.AddFundsAsync(
            request.UserId,
            request.Amount,
            request.Description,
            request.ReferenceId,
            cancellationToken);
    }
}

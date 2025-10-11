using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.ManageWallet;

/// <summary>Handler for CreateWalletCommand</summary>
public class CreateWalletHandler : IRequestHandler<CreateWalletCommand, UserWallet>
{
    private readonly IWalletService _walletService;

    public CreateWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<UserWallet> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        return await _walletService.CreateWalletAsync(request.UserId, request.Currency, cancellationToken);
    }
}

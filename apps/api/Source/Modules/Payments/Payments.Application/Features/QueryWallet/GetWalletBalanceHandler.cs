using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryWallet;

/// <summary>Handler for GetWalletBalanceQuery</summary>
public class GetWalletBalanceHandler : IRequestHandler<GetWalletBalanceQuery, decimal>
{
    private readonly IWalletService _walletService;

    public GetWalletBalanceHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<decimal> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        return await _walletService.GetBalanceAsync(request.UserId, cancellationToken);
    }
}

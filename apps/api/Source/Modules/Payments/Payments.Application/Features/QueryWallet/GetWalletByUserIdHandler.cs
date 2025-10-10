using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryWallet;

/// <summary>Handler for GetWalletByUserIdQuery</summary>
public class GetWalletByUserIdHandler : IRequestHandler<GetWalletByUserIdQuery, UserWallet?>
{
    private readonly IWalletService _walletService;

    public GetWalletByUserIdHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<UserWallet?> Handle(GetWalletByUserIdQuery request, CancellationToken cancellationToken)
    {
        return await _walletService.GetWalletByUserIdAsync(request.UserId, cancellationToken);
    }
}

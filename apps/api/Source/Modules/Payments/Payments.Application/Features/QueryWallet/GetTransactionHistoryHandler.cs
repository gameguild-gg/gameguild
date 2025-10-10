using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryWallet;

/// <summary>Handler for GetTransactionHistoryQuery</summary>
public class GetTransactionHistoryHandler : IRequestHandler<GetTransactionHistoryQuery, List<WalletTransaction>>
{
    private readonly IWalletService _walletService;

    public GetTransactionHistoryHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<List<WalletTransaction>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _walletService.GetTransactionHistoryAsync(
            request.UserId,
            request.Skip,
            request.Take,
            request.TypeFilter,
            request.StatusFilter,
            cancellationToken);
    }
}

using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetWalletBalanceQuery
/// </summary>
public sealed class GetWalletBalanceQueryHandler(IWalletService walletService) : IQueryHandler<GetWalletBalanceQuery, decimal>
{
    public async Task<decimal> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken) { return await walletService.GetBalanceAsync(request.UserId, cancellationToken); }
}

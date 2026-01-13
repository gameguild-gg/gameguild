using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetWalletByUserIdQuery
/// </summary>
public sealed class GetWalletByUserIdQueryHandler(IWalletService walletService) : IQueryHandler<GetWalletByUserIdQuery, UserWallet?>
{
    public async Task<UserWallet?> Handle(GetWalletByUserIdQuery request, CancellationToken cancellationToken) { return await walletService.GetWalletByUserIdAsync(request.UserId, cancellationToken); }
}

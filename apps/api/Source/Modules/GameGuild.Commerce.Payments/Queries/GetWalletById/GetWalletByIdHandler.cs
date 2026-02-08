using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.GetWalletById;

/// <summary>
///     Handler for GetWalletByIdQuery
/// </summary>
public sealed class GetWalletByIdHandler(IWalletService walletService) : IQueryHandler<GetWalletByIdQuery, UserWallet?>
{
    public async Task<UserWallet?> Handle(GetWalletByIdQuery request, CancellationToken cancellationToken)
    {
        return await walletService.GetWalletByIdAsync(request.WalletId, cancellationToken).ConfigureAwait(false);
    }
}

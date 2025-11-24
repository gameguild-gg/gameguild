using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetWalletByUserIdQuery
/// </summary>
public sealed class GetWalletByUserIdQueryHandler(IWalletService walletService) : IQueryHandler<GetWalletByUserIdQuery, UserWallet?>
{
    public async Task<UserWallet?> Handle(GetWalletByUserIdQuery request, CancellationToken cancellationToken) { return await walletService.GetWalletByUserIdAsync(request.UserId, cancellationToken); }
}

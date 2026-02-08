using GameGuild.Commerce.Payments.Models;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.ListWallets;

/// <summary>
///     Handler for ListWalletsQuery
/// </summary>
public sealed class ListWalletsHandler(IWalletService walletService) : IQueryHandler<ListWalletsQuery, WalletListResponse>
{
    public async Task<WalletListResponse> Handle(ListWalletsQuery request, CancellationToken cancellationToken)
    {
        var (wallets, totalCount) = await walletService.ListWalletsAsync(
            request.Page,
            request.PageSize,
            request.Currency,
            request.IsFrozen,
            cancellationToken).ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var items = wallets.Select(w => new WalletSummary(
            w.Id,
            w.UserId,
            w.Currency,
            w.Balance,
            w.IsLocked,
            w.CreatedAt,
            w.LastTransactionAt
        )).ToList();

        return new WalletListResponse(items, totalCount, request.Page, request.PageSize, totalPages);
    }
}

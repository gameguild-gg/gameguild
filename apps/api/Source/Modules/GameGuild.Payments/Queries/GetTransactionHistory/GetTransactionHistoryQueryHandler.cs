using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetTransactionHistoryQuery
/// </summary>
public sealed class GetTransactionHistoryQueryHandler(IWalletService walletService) : IQueryHandler<GetTransactionHistoryQuery, List<WalletTransaction>>
{
    public async Task<List<WalletTransaction>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await walletService.GetTransactionHistoryAsync(request.UserId, request.Skip, request.Take, request.TypeFilter, request.StatusFilter, cancellationToken).ConfigureAwait(false);
    }
}

using GameGuild.Commerce.Payments.Controllers;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments.Queries.GetWalletAuditLog;

/// <summary>
///     Handler for GetWalletAuditLogQuery
/// </summary>
public sealed class GetWalletAuditLogHandler(IWalletService walletService) : IQueryHandler<GetWalletAuditLogQuery, WalletAuditLogResponse>
{
    public async Task<WalletAuditLogResponse> Handle(GetWalletAuditLogQuery request, CancellationToken cancellationToken)
    {
        var (transactions, totalCount) = await walletService.GetWalletAuditLogAsync(
            request.WalletId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var items = transactions.Select(t => new WalletAuditEntry(
            t.Id,
            t.WalletId,
            t.Type.ToString(),
            t.Description,
            t.Amount,
            t.BalanceAfter,
            t.CreatedAt,
            null // PerformedBy would come from audit context
        )).ToList();

        return new WalletAuditLogResponse(items, totalCount, request.Page, request.PageSize, totalPages);
    }
}

using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetLedgerEntriesByAccountQuery
/// </summary>
public sealed class GetLedgerEntriesByAccountQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetLedgerEntriesByAccountQuery, List<FinancialLedgerEntry>>
{
    public async Task<List<FinancialLedgerEntry>> Handle(GetLedgerEntriesByAccountQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetLedgerEntriesByAccountAsync(request.Account, request.Skip, request.Take, cancellationToken);
    }
}

using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetUnreconciledLedgerEntriesQuery
/// </summary>
public sealed class GetUnreconciledLedgerEntriesQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetUnreconciledLedgerEntriesQuery, List<FinancialLedgerEntry>>
{
    public async Task<List<FinancialLedgerEntry>> Handle(GetUnreconciledLedgerEntriesQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetUnreconciledEntriesAsync(request.Skip, request.Take, cancellationToken).ConfigureAwait(false);
    }
}

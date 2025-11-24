using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

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

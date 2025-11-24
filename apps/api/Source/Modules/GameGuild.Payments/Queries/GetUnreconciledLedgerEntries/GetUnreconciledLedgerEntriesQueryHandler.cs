using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetUnreconciledLedgerEntriesQuery
/// </summary>
public sealed class GetUnreconciledLedgerEntriesQueryHandler(IRevenueAuditService revenueAuditService) : IQueryHandler<GetUnreconciledLedgerEntriesQuery, List<FinancialLedgerEntry>>
{
    public async Task<List<FinancialLedgerEntry>> Handle(GetUnreconciledLedgerEntriesQuery request, CancellationToken cancellationToken)
    {
        return await revenueAuditService.GetUnreconciledEntriesAsync(request.Skip, request.Take, cancellationToken);
    }
}

using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Queries;

public class GetUnreconciledEntriesQueryHandler : IRequestHandler<GetUnreconciledEntriesQuery, List<FinancialLedgerEntry>>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public GetUnreconciledEntriesQueryHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<List<FinancialLedgerEntry>> Handle(GetUnreconciledEntriesQuery request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.GetUnreconciledEntriesAsync(
            request.Skip,
            request.Take,
            cancellationToken);
    }
}

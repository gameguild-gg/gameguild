using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Queries;

public class GetLedgerEntriesByAccountQueryHandler : IRequestHandler<GetLedgerEntriesByAccountQuery, List<FinancialLedgerEntry>>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public GetLedgerEntriesByAccountQueryHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<List<FinancialLedgerEntry>> Handle(GetLedgerEntriesByAccountQuery request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.GetLedgerEntriesByAccountAsync(
            request.Account,
            request.Skip,
            request.Take,
            cancellationToken);
    }
}

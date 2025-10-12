using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public class CreateLedgerEntryCommandHandler : IRequestHandler<CreateLedgerEntryCommand, FinancialLedgerEntry>
{
    private readonly IRevenueAuditService _revenueAuditService;

    public CreateLedgerEntryCommandHandler(IRevenueAuditService revenueAuditService)
    {
        _revenueAuditService = revenueAuditService;
    }

    public async Task<FinancialLedgerEntry> Handle(CreateLedgerEntryCommand request, CancellationToken cancellationToken)
    {
        return await _revenueAuditService.CreateLedgerEntryAsync(
            request.EntryType,
            request.DebitAccount,
            request.CreditAccount,
            request.Amount,
            request.Currency,
            request.Description,
            request.RevenueEventId,
            request.ReferenceNumber,
            cancellationToken);
    }
}

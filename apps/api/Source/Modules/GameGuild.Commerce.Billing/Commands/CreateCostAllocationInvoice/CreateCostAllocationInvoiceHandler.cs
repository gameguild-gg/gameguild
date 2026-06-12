using GameGuild.Billing;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

public sealed class CreateCostAllocationInvoiceHandler(IApplicationDbContext context)
    : ICommandHandler<CreateCostAllocationInvoiceCommand, CostAllocationInvoiceResult>
{
    public async Task<CostAllocationInvoiceResult> Handle(CreateCostAllocationInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(request));
        }

        if (request.SubscriptionId == Guid.Empty)
        {
            throw new ArgumentException("SubscriptionId is required.", nameof(request));
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));
        }

        var invoice = new Invoice(request.TenantId, request.SubscriptionId, request.Amount, request.Currency);
        invoice.SetBillingPeriod(request.PeriodStart, request.PeriodEnd);
        invoice.Issue(request.DueDate);

        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CostAllocationInvoiceResult(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status.ToString(),
            invoice.Total,
            invoice.DueDate);
    }
}

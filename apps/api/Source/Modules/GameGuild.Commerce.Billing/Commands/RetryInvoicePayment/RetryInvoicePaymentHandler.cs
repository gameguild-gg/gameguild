using Microsoft.EntityFrameworkCore;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

public sealed class RetryInvoicePaymentHandler(IApplicationDbContext context)
    : ICommandHandler<RetryInvoicePaymentCommand, InvoicePaymentRetryResult>
{
    public async Task<InvoicePaymentRetryResult> Handle(RetryInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await context.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new InvoicePaymentRetryResult(
                request.InvoiceId,
                string.Empty,
                InvoiceStatus.Draft,
                Accepted: false,
                Code: "NotFound",
                Message: "Invoice was not found.",
                RetryScheduledAt: null);
        }

        if (invoice.Status is InvoiceStatus.PastDue or InvoiceStatus.Open)
        {
            return new InvoicePaymentRetryResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                Accepted: true,
                Code: "RetryAccepted",
                Message: "Invoice payment retry was accepted for local scheduling. External gateway capture requires provider configuration.",
                RetryScheduledAt: SystemClock.UtcNow);
        }

        var code = invoice.Status switch
        {
            InvoiceStatus.Uncollectible => "RetryLimitExceeded",
            InvoiceStatus.Paid => "AlreadyPaid",
            InvoiceStatus.Void => "Voided",
            InvoiceStatus.Draft => "InvoiceNotIssued",
            _ => "RetryRejected"
        };

        return new InvoicePaymentRetryResult(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status,
            Accepted: false,
            code,
            $"Invoice payment retry is not allowed while the invoice is {invoice.Status}.",
            RetryScheduledAt: null);
    }
}

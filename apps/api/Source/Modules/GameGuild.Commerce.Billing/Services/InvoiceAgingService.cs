using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Invoice aging boundary (issue #1205 G-2): moves <see cref="InvoiceStatus.Open" />
///     invoices past their due date to <see cref="InvoiceStatus.PastDue" />. The sweep is
///     invoked on-next-touch (invoice materialization, manual retry) rather than by a
///     dedicated worker — no invoice-aging background job exists in the module yet.
/// </summary>
public interface IInvoiceAgingService
{
    /// <summary>
    ///     Marks the subscription's overdue open invoices PastDue. When
    ///     <paramref name="subscriptionId" /> is null every overdue open invoice in scope
    ///     is aged. Returns the ids of the invoices that transitioned.
    /// </summary>
    Task<IReadOnlyList<Guid>> MarkOverdueOpenInvoicesPastDueAsync(
        Guid? subscriptionId = null,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class InvoiceAgingService(
    IApplicationDbContext context,
    ILogger<InvoiceAgingService> logger) : IInvoiceAgingService
{
    public async Task<IReadOnlyList<Guid>> MarkOverdueOpenInvoicesPastDueAsync(
        Guid? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;

        var query = context.Set<Invoice>()
            .Where(invoice => invoice.Status == InvoiceStatus.Open && invoice.DueDate < now);

        if (subscriptionId.HasValue)
        {
            query = query.Where(invoice => invoice.SubscriptionId == subscriptionId.Value);
        }

        var overdue = await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var invoice in overdue)
        {
            try
            {
                invoice.MarkPastDue();
            }
            catch (InvalidOperationException exception)
            {
                // A concurrent touch already moved the invoice; skip it.
                logger.LogDebug(exception,
                    "Invoice {InvoiceId} was concurrently transitioned while aging; skipping.",
                    invoice.Id);
            }
        }

        if (overdue.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Aged {Count} overdue open invoice(s) to PastDue{Scope}.",
                overdue.Count,
                subscriptionId.HasValue ? $" for subscription {subscriptionId.Value}" : string.Empty);
        }

        return overdue.Select(invoice => invoice.Id).ToList();
    }
}

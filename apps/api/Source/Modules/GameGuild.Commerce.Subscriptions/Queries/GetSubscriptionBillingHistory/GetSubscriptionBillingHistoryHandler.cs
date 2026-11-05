using GameGuild.CQRS;
using GameGuild.Commerce.Payments;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for <see cref="GetSubscriptionBillingHistoryQuery" /> (issue #1205 G-8):
///     projects the shared invoice read model (plus the payment's external reference when
///     bound) into the billing-history DTO, newest first. Mirrors the
///     <see cref="GetSubscriptionInvoicesHandler" /> read path.
/// </summary>
public sealed class GetSubscriptionBillingHistoryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionBillingHistoryQuery, IEnumerable<BillingHistoryDto>>
{
    public async Task<IEnumerable<BillingHistoryDto>> Handle(
        GetSubscriptionBillingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var subscriptionExists = await context.Set<Subscription>()
            .AnyAsync(s => s.Id == request.SubscriptionId, cancellationToken).ConfigureAwait(false);

        if (!subscriptionExists)
        {
            return [];
        }

        var invoices = await context.Set<SubscriptionInvoiceReadModel>()
            .AsNoTracking()
            .Where(invoice => invoice.SubscriptionId == request.SubscriptionId)
            .OrderByDescending(invoice => invoice.IssuedAt ?? invoice.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var paymentIds = invoices
            .Where(invoice => invoice.PaymentId.HasValue)
            .Select(invoice => invoice.PaymentId!.Value)
            .Distinct()
            .ToList();

        var externalPaymentIds = paymentIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await context.Set<Payment>()
                .AsNoTracking()
                .Where(payment => paymentIds.Contains(payment.Id))
                .ToDictionaryAsync(payment => payment.Id, payment => payment.ExternalPaymentId, cancellationToken)
                .ConfigureAwait(false);

        return invoices.Select(invoice => new BillingHistoryDto
        {
            Id = invoice.Id,
            SubscriptionId = invoice.SubscriptionId,
            BillingDate = invoice.IssuedAt ?? invoice.CreatedAt,
            Amount = invoice.Total,
            Currency = invoice.Currency,
            Status = MapInvoiceStatus(invoice.Status),
            ExternalPaymentId = invoice.PaymentId.HasValue &&
                                externalPaymentIds.TryGetValue(invoice.PaymentId.Value, out var externalPaymentId)
                ? externalPaymentId
                : null,
            Description = $"Invoice {invoice.InvoiceNumber}",
            CreatedAt = invoice.CreatedAt
        }).ToList();
    }

    private static string MapInvoiceStatus(int status) => status switch
    {
        0 => "Draft",
        1 => "Open",
        2 => "Paid",
        3 => "Void",
        4 => "PastDue",
        5 => "Uncollectible",
        _ => "Unknown"
    };
}

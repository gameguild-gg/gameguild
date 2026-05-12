using GameGuild.CQRS;
using GameGuild.Commerce.Payments;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for GetSubscriptionInvoicesQuery
/// </summary>
public sealed class GetSubscriptionInvoicesHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionInvoicesQuery, PagedResult<SubscriptionInvoiceDto>>
{
    public async Task<PagedResult<SubscriptionInvoiceDto>> Handle(
        GetSubscriptionInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var subscriptionExists = await context.Set<Subscription>()
            .AnyAsync(s => s.Id == request.SubscriptionId, cancellationToken).ConfigureAwait(false);

        if (!subscriptionExists)
        {
            return new PagedResult<SubscriptionInvoiceDto>([], 0, (request.Page - 1) * request.PageSize, request.PageSize);
        }

        var skip = (request.Page - 1) * request.PageSize;

        var invoicesQuery = context.Set<SubscriptionInvoiceReadModel>()
            .AsNoTracking()
            .Where(invoice => invoice.SubscriptionId == request.SubscriptionId);

        var totalCount = await invoicesQuery
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var invoices = await invoicesQuery
            .OrderByDescending(invoice => invoice.IssuedAt ?? invoice.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var paymentIds = invoices
            .Where(invoice => invoice.PaymentId.HasValue)
            .Select(invoice => invoice.PaymentId!.Value)
            .Distinct()
            .ToList();

        var paymentMethods = paymentIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await context.Set<Payment>()
                .AsNoTracking()
                .Where(payment => paymentIds.Contains(payment.Id))
                .ToDictionaryAsync(payment => payment.Id, payment => payment.PaymentMethodId, cancellationToken)
                .ConfigureAwait(false);

        var items = invoices.Select(invoice => new SubscriptionInvoiceDto(
            invoice.Id,
            invoice.SubscriptionId,
            invoice.InvoiceNumber,
            invoice.Total,
            invoice.Currency,
            invoice.IssuedAt ?? invoice.CreatedAt,
            invoice.DueDate,
            invoice.PaidAt,
            MapInvoiceStatus(invoice.Status),
            invoice.PaymentId.HasValue && paymentMethods.TryGetValue(invoice.PaymentId.Value, out var paymentMethod)
                ? paymentMethod
                : null,
            invoice.ExternalId)).ToList();

        return new PagedResult<SubscriptionInvoiceDto>(
            items,
            totalCount,
            skip,
            request.PageSize);
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

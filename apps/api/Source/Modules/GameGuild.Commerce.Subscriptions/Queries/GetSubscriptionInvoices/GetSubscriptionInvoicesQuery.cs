using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get subscription invoices
/// </summary>
public record GetSubscriptionInvoicesQuery(Guid SubscriptionId, int Page = 1, int PageSize = 20) 
    : IQuery<PagedResult<SubscriptionInvoiceDto>>;

/// <summary>
///     DTO for subscription invoice
/// </summary>
public record SubscriptionInvoiceDto(
    Guid Id,
    Guid SubscriptionId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateTime InvoiceDate,
    DateTime? DueDate,
    DateTime? PaidDate,
    string Status,
    string? PaymentMethod,
    string? ExternalInvoiceId);

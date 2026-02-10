namespace GameGuild.Commerce.Billing;

/// <summary>
///     Unified webhook event model for internal processing.
///     Provides a normalized view across all payment providers.
///     Use this for logging, auditing, and cross-provider operations.
/// </summary>
public sealed class UnifiedWebhookEvent
{
    /// <summary>
    ///     Payment provider that sent the webhook.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    ///     Event type (payment.success, subscription.created, etc.)
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    ///     Provider's unique event identifier (for idempotency).
    /// </summary>
    public required string EventId { get; init; }

    /// <summary>
    ///     Tenant ID extracted from the event.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    ///     External subscription ID if applicable.
    /// </summary>
    public string? ExternalSubscriptionId { get; init; }

    /// <summary>
    ///     External payment/transaction ID if applicable.
    /// </summary>
    public string? ExternalPaymentId { get; init; }

    /// <summary>
    ///     Normalized status (success, failed, pending, etc.)
    /// </summary>
    public WebhookEventStatus Status { get; init; }

    /// <summary>
    ///     Amount involved if applicable.
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    ///     Currency code.
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    ///     When the event occurred at the provider.
    /// </summary>
    public DateTime EventTimestamp { get; init; }

    /// <summary>
    ///     When we received the webhook.
    /// </summary>
    public DateTime ReceivedAt { get; init; } = SystemClock.UtcNow;

    /// <summary>
    ///     Raw payload for debugging/auditing.
    /// </summary>
    public string? RawPayload { get; init; }

    /// <summary>
    ///     Optional error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Additional provider-specific data.
    /// </summary>
    public Dictionary<string, object>? ProviderData { get; init; }

    /// <summary>
    ///     Creates from a Stripe payment payload.
    /// </summary>
    public static UnifiedWebhookEvent FromStripePayment(
        StripePaymentWebhookPayload payload,
        string eventType,
        string eventId)
    {
        return new UnifiedWebhookEvent
        {
            Provider = PaymentProviders.Stripe,
            EventType = eventType,
            EventId = eventId,
            TenantId = payload.TenantId,
            ExternalSubscriptionId = payload.ExternalSubscriptionId,
            ExternalPaymentId = payload.PaymentId,
            Status = MapStatus(payload.Status),
            Amount = payload.Amount,
            Currency = payload.Currency,
            EventTimestamp = payload.PaidAt ?? SystemClock.UtcNow,
            ProviderData = new Dictionary<string, object>
            {
                ["customerId"] = payload.CustomerId ?? "",
                ["invoiceId"] = payload.InvoiceId ?? "",
                ["chargeId"] = payload.ChargeId ?? ""
            }
        };
    }

    /// <summary>
    ///     Creates from a PayPal payment payload.
    /// </summary>
    public static UnifiedWebhookEvent FromPayPalPayment(
        PayPalPaymentWebhookPayload payload,
        string eventType,
        string eventId)
    {
        return new UnifiedWebhookEvent
        {
            Provider = PaymentProviders.PayPal,
            EventType = eventType,
            EventId = eventId,
            TenantId = payload.TenantId,
            ExternalSubscriptionId = payload.ExternalSubscriptionId,
            ExternalPaymentId = payload.PaymentId,
            Status = MapStatus(payload.Status),
            Amount = payload.Amount,
            Currency = payload.Currency,
            EventTimestamp = payload.PaidAt ?? SystemClock.UtcNow,
            ProviderData = new Dictionary<string, object>
            {
                ["transactionId"] = payload.TransactionId ?? "",
                ["payerId"] = payload.PayerId ?? "",
                ["isRefund"] = payload.IsRefund
            }
        };
    }

    /// <summary>
    ///     Creates from a Stripe subscription payload.
    /// </summary>
    public static UnifiedWebhookEvent FromStripeSubscription(
        StripeSubscriptionWebhookPayload payload,
        string eventType,
        string eventId)
    {
        return new UnifiedWebhookEvent
        {
            Provider = PaymentProviders.Stripe,
            EventType = eventType,
            EventId = eventId,
            TenantId = payload.TenantId,
            ExternalSubscriptionId = payload.ExternalSubscriptionId,
            Status = MapStatus(payload.Status),
            Amount = payload.Amount,
            EventTimestamp = payload.StartDate ?? SystemClock.UtcNow,
            ProviderData = new Dictionary<string, object>
            {
                ["customerId"] = payload.CustomerId ?? "",
                ["productId"] = payload.ProductId ?? "",
                ["priceId"] = payload.PriceId ?? "",
                ["interval"] = payload.Interval ?? "",
                ["cancelAtPeriodEnd"] = payload.CancelAtPeriodEnd
            }
        };
    }

    private static WebhookEventStatus MapStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "succeeded" or "success" or "paid" or "completed" => WebhookEventStatus.Success,
            "failed" or "failure" or "declined" => WebhookEventStatus.Failed,
            "pending" or "processing" => WebhookEventStatus.Pending,
            "canceled" or "cancelled" => WebhookEventStatus.Canceled,
            "refunded" => WebhookEventStatus.Refunded,
            _ => WebhookEventStatus.Unknown
        };
    }
}

/// <summary>
///     Normalized webhook event status across all providers.
/// </summary>
public enum WebhookEventStatus
{
    Unknown,
    Success,
    Failed,
    Pending,
    Canceled,
    Refunded
}

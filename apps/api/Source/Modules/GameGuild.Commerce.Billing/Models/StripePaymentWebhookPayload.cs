namespace GameGuild.Commerce.Billing;

/// <summary>
///     Stripe-specific payment webhook payload.
///     Concrete implementation of the abstract PaymentWebhookPayload.
/// </summary>
public sealed class StripePaymentWebhookPayload : PaymentWebhookPayload
{
    /// <summary>
    ///     Stripe Customer ID (cus_xxxxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    ///     Stripe Invoice ID (in_xxxxx)
    /// </summary>
    public string? InvoiceId { get; set; }

    /// <summary>
    ///     Stripe Charge ID (ch_xxxxx)
    /// </summary>
    public string? ChargeId { get; set; }

    /// <summary>
    ///     Payment method type (card, bank_transfer, etc.)
    /// </summary>
    public string? PaymentMethodType { get; set; }

    /// <summary>
    ///     Last 4 digits of the card if applicable
    /// </summary>
    public string? Last4 { get; set; }

    /// <summary>
    ///     Card brand if applicable (visa, mastercard, etc.)
    /// </summary>
    public string? CardBrand { get; set; }

    /// <summary>
    ///     Receipt URL from Stripe
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    ///     Billing reason (subscription_create, subscription_cycle, manual, etc.)
    /// </summary>
    public string? BillingReason { get; set; }
}

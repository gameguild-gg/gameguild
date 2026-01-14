namespace GameGuild.Commerce.Billing;

/// <summary>
///     Stripe-specific subscription webhook payload.
///     Concrete implementation of the abstract SubscriptionWebhookPayload.
/// </summary>
public sealed class StripeSubscriptionWebhookPayload : SubscriptionWebhookPayload
{
    /// <summary>
    ///     Stripe Customer ID (cus_xxxxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    ///     Stripe Product ID (prod_xxxxx)
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    ///     Stripe Price ID (price_xxxxx)
    /// </summary>
    public string? PriceId { get; set; }

    /// <summary>
    ///     Billing interval (month, year, etc.)
    /// </summary>
    public string? Interval { get; set; }

    /// <summary>
    ///     Trial end date if subscription has trial
    /// </summary>
    public DateTime? TrialEnd { get; set; }

    /// <summary>
    ///     Whether to cancel at period end
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    ///     Collection method (charge_automatically, send_invoice)
    /// </summary>
    public string? CollectionMethod { get; set; }
}

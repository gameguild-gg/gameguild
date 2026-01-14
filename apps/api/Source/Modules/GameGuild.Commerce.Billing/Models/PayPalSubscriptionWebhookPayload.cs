namespace GameGuild.Commerce.Billing;

/// <summary>
///     PayPal-specific subscription webhook payload.
///     Concrete implementation of the abstract SubscriptionWebhookPayload.
/// </summary>
public sealed class PayPalSubscriptionWebhookPayload : SubscriptionWebhookPayload
{
    /// <summary>
    ///     PayPal Payer ID
    /// </summary>
    public string? PayerId { get; set; }

    /// <summary>
    ///     PayPal Plan ID (billing agreement)
    /// </summary>
    public string? PayPalPlanId { get; set; }

    /// <summary>
    ///     PayPal Billing Agreement ID
    /// </summary>
    public string? BillingAgreementId { get; set; }

    /// <summary>
    ///     Billing frequency (DAY, WEEK, MONTH, YEAR)
    /// </summary>
    public string? BillingFrequency { get; set; }

    /// <summary>
    ///     Number of billing cycles
    /// </summary>
    public int? BillingCycles { get; set; }
}

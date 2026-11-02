namespace GameGuild.Commerce.Billing;

/// <summary>
///     Braintree-specific subscription webhook payload (issue #408). Concrete implementation
///     of the abstract <see cref="SubscriptionWebhookPayload"/> carrying the routing
///     projection of a verified Braintree notification.
/// </summary>
public sealed class BraintreeSubscriptionWebhookPayload : SubscriptionWebhookPayload
{
    /// <summary>Braintree subscription status as reported by the provider.</summary>
    public string? BraintreeStatus { get; set; }
}

/// <summary>
///     Braintree-specific payment webhook payload (issue #408). Concrete implementation
///     of the abstract <see cref="PaymentWebhookPayload"/> carrying the routing projection
///     of a verified Braintree notification.
/// </summary>
public sealed class BraintreePaymentWebhookPayload : PaymentWebhookPayload
{
    /// <summary>Braintree transaction id of the charge, when the subject carried one.</summary>
    public string? TransactionId { get; set; }

    /// <summary>Braintree transaction status as reported by the provider.</summary>
    public string? TransactionStatus { get; set; }
}

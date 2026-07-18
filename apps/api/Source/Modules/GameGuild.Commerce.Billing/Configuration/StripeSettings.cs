namespace GameGuild.Commerce.Billing;

/// <summary>
///     Stripe-specific configuration
/// </summary>
public class StripeSettings
{
    /// <summary>
    ///     Stripe API secret key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     Stripe publishable key
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    ///     Stripe webhook signing secret
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    ///     API version to use
    /// </summary>
    public string ApiVersion { get; set; } = "2023-10-16";

    /// <summary>Stable identifier for the Stripe webhook endpoint receiving the event.</summary>
    public string WebhookEndpointId { get; set; } = string.Empty;

    /// <summary>Expected Stripe Connect account. Empty identifies the platform account endpoint.</summary>
    public string ConnectedAccountId { get; set; } = string.Empty;

    /// <summary>Whether this endpoint accepts live-mode events.</summary>
    public bool LiveMode { get; set; }

    /// <summary>Maximum accepted Stripe signature age in seconds.</summary>
    public long WebhookToleranceSeconds { get; set; } = 300;
}

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
}

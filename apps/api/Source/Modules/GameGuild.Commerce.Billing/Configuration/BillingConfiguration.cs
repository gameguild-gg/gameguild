namespace GameGuild.Commerce.Billing;

/// <summary>
///     Configuration settings for Billing module
/// </summary>
public abstract class BillingConfiguration
{
    /// <summary>
    ///     Configuration section name
    /// </summary>
    public const string SectionName = "Billing";

    /// <summary>
    ///     Stripe configuration settings
    /// </summary>
    public StripeSettings Stripe { get; set; } = new StripeSettings();

    /// <summary>
    ///     PayPal configuration settings
    /// </summary>
    public PayPalSettings PayPal { get; set; } = new PayPalSettings();

    /// <summary>
    ///     Apple Pay configuration settings
    /// </summary>
    public ApplePaySettings ApplePay { get; set; } = new ApplePaySettings();

    /// <summary>
    ///     Webhook configuration settings
    /// </summary>
    public WebhookSettings Webhook { get; set; } = new WebhookSettings();
}

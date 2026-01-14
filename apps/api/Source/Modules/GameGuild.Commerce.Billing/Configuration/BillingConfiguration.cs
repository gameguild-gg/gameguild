namespace GameGuild.Commerce.Billing;

/// <summary>
///     Configuration settings for Billing module.
///     Contains all payment provider settings and shared configuration logic.
/// </summary>
public class BillingConfiguration
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

    /// <summary>
    ///     Gets the list of enabled payment providers based on configuration.
    /// </summary>
    public IEnumerable<string> GetEnabledProviders()
    {
        if (!string.IsNullOrEmpty(Stripe.SecretKey))
            yield return PaymentProviders.Stripe;

        if (!string.IsNullOrEmpty(PayPal.ClientId))
            yield return PaymentProviders.PayPal;

        if (!string.IsNullOrEmpty(ApplePay.BundleId))
            yield return PaymentProviders.AppleAppStore;
    }

    /// <summary>
    ///     Checks if a specific payment provider is configured.
    /// </summary>
    public bool IsProviderEnabled(string provider)
    {
        return provider switch
        {
            PaymentProviders.Stripe => !string.IsNullOrEmpty(Stripe.SecretKey),
            PaymentProviders.PayPal => !string.IsNullOrEmpty(PayPal.ClientId),
            PaymentProviders.AppleAppStore => !string.IsNullOrEmpty(ApplePay.BundleId),
            _ => false
        };
    }

    /// <summary>
    ///     Gets the webhook secret for a specific provider.
    /// </summary>
    public string? GetWebhookSecret(string provider)
    {
        return provider switch
        {
            PaymentProviders.Stripe => Stripe.WebhookSecret,
            PaymentProviders.PayPal => PayPal.WebhookId, // PayPal uses webhook ID for verification
            _ => null
        };
    }

    /// <summary>
    ///     Validates that required configuration is present for the specified provider.
    /// </summary>
    public BillingConfigurationValidationResult Validate(string? provider = null)
    {
        var errors = new List<string>();

        if (provider == null || provider == PaymentProviders.Stripe)
        {
            if (!string.IsNullOrEmpty(Stripe.SecretKey) && string.IsNullOrEmpty(Stripe.PublishableKey))
                errors.Add("Stripe: PublishableKey is required when SecretKey is set");
        }

        if (provider == null || provider == PaymentProviders.PayPal)
        {
            if (!string.IsNullOrEmpty(PayPal.ClientId) && string.IsNullOrEmpty(PayPal.ClientSecret))
                errors.Add("PayPal: ClientSecret is required when ClientId is set");
        }

        if (provider == null || provider == PaymentProviders.AppleAppStore)
        {
            if (!string.IsNullOrEmpty(ApplePay.BundleId) && string.IsNullOrEmpty(ApplePay.SharedSecret))
                errors.Add("ApplePay: SharedSecret is required when BundleId is set");
        }

        return new BillingConfigurationValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
///     Result of billing configuration validation.
/// </summary>
public sealed record BillingConfigurationValidationResult(bool IsValid, IReadOnlyList<string> Errors);

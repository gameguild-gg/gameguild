namespace GameGuild.Commerce.Billing;

/// <summary>
///     PayPal-specific configuration
/// </summary>
public class PayPalSettings
{
    /// <summary>
    ///     PayPal client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     PayPal client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    ///     PayPal webhook ID
    /// </summary>
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>
    ///     PayPal environment (sandbox or live)
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    ///     Base URL for PayPal API
    /// </summary>
    public string BaseUrl { get => Environment.ToLowerInvariant() == "live" ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com"; }
}

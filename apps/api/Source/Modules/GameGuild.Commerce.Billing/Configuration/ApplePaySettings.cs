namespace GameGuild.Commerce.Billing;

/// <summary>
///     Apple Pay / App Store configuration settings.
/// </summary>
public class ApplePaySettings
{
    /// <summary>
    ///     Your app's bundle ID (e.g., "com.example.myapp").
    /// </summary>
    public string BundleId { get; set; } = string.Empty;

    /// <summary>
    ///     Apple Team ID (from Apple Developer Portal).
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    ///     Key ID for the App Store Connect API key.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    ///     Path to the App Store Connect API private key (.p8 file).
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    ///     The App Store Connect API private key content (alternative to PrivateKeyPath).
    /// </summary>
    public string? PrivateKeyContent { get; set; }

    /// <summary>
    ///     Shared secret for App Store receipt validation (legacy API).
    ///     Note: Prefer using App Store Server API with PrivateKey for new integrations.
    /// </summary>
    public string? SharedSecret { get; set; }

    /// <summary>
    ///     Environment: "production" or "sandbox".
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    ///     Gets the App Store Server API base URL based on environment.
    /// </summary>
    public string BaseUrl => Environment.ToLowerInvariant() == "production"
        ? "https://api.storekit.itunes.apple.com"
        : "https://api.storekit-sandbox.itunes.apple.com";

    /// <summary>
    ///     Allowed merchant IDs for Apple Pay webhooks.
    /// </summary>
    public List<string> AllowedMerchantIds { get; set; } = [];
}

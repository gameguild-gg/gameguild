namespace GameGuild.Commerce;

/// <summary>
///     Constants for payment provider identifiers.
///     Use these instead of magic strings throughout the commerce modules.
/// </summary>
public static class PaymentProviders
{
    /// <summary>Stripe payment provider</summary>
    public const string Stripe = "stripe";

    /// <summary>PayPal payment provider</summary>
    public const string PayPal = "paypal";

    /// <summary>Apple Pay payment provider (in-app purchases)</summary>
    public const string ApplePay = "applepay";

    /// <summary>Apple App Store (server-to-server notifications)</summary>
    public const string AppleAppStore = "apple_app_store";

    /// <summary>Google Pay payment provider</summary>
    public const string GooglePay = "googlepay";

    /// <summary>Google Play Store (server-to-server notifications)</summary>
    public const string GooglePlayStore = "google_play_store";

    /// <summary>
    ///     All supported payment providers.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        Stripe,
        PayPal,
        ApplePay,
        AppleAppStore,
        GooglePay,
        GooglePlayStore
    };

    /// <summary>
    ///     Validates if a provider string is a supported payment provider.
    /// </summary>
    /// <param name="provider">The provider string to validate</param>
    /// <returns>True if the provider is supported, false otherwise</returns>
    public static bool IsSupported(string? provider) =>
        !string.IsNullOrEmpty(provider) && All.Contains(provider.ToLowerInvariant());

    /// <summary>
    ///     Normalizes a provider string to lowercase.
    /// </summary>
    /// <param name="provider">The provider string to normalize</param>
    /// <returns>The normalized provider string</returns>
    public static string Normalize(string provider) =>
        provider.ToLowerInvariant();
}

/// <summary>
///     Constants for currency codes (ISO 4217).
/// </summary>
public static class CurrencyCodes
{
    /// <summary>US Dollar</summary>
    public const string USD = "USD";

    /// <summary>Euro</summary>
    public const string EUR = "EUR";

    /// <summary>British Pound</summary>
    public const string GBP = "GBP";

    /// <summary>Japanese Yen</summary>
    public const string JPY = "JPY";

    /// <summary>Canadian Dollar</summary>
    public const string CAD = "CAD";

    /// <summary>Australian Dollar</summary>
    public const string AUD = "AUD";

    /// <summary>Default currency code</summary>
    public const string Default = USD;

    /// <summary>
    ///     All supported currency codes.
    /// </summary>
    public static readonly IReadOnlyList<string> Supported = new[]
    {
        USD, EUR, GBP, JPY, CAD, AUD
    };

    /// <summary>
    ///     Validates if a currency code is supported.
    /// </summary>
    /// <param name="currencyCode">The currency code to validate</param>
    /// <returns>True if the currency code is supported, false otherwise</returns>
    public static bool IsSupported(string? currencyCode) =>
        !string.IsNullOrEmpty(currencyCode) && Supported.Contains(currencyCode.ToUpperInvariant());
}

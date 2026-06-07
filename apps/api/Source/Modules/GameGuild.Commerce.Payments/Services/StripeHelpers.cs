using System.Text.RegularExpressions;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Converts between decimal amounts and Stripe's integer (smallest-currency-unit) format.
/// </summary>
internal static class StripeAmountConverter
{
    private static readonly HashSet<string> ZeroDecimalCurrencies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF",
            "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

    /// <summary>
    ///     Converts a decimal amount to Stripe's integer format (cents / smallest currency unit).
    /// </summary>
    public static long ToStripeAmount(decimal amount, string currency) =>
        ZeroDecimalCurrencies.Contains(currency) ? (long)amount : (long)(amount * 100);

    /// <summary>
    ///     Converts a Stripe integer amount back to decimal.
    /// </summary>
    public static decimal FromStripeAmount(long amount, string currency) =>
        ZeroDecimalCurrencies.Contains(currency) ? amount : amount / 100m;
}

/// <summary>
///     Maps between Stripe status strings and domain enums.
/// </summary>
internal static class StripeStatusMapper
{
    /// <summary>
    ///     Maps a Stripe payment-intent status to <see cref="PaymentStatus"/>.
    /// </summary>
    public static PaymentStatus MapPaymentStatus(string stripeStatus) =>
        stripeStatus switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "processing" => PaymentStatus.Processing,
            "requires_payment_method" => PaymentStatus.Failed,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.RequiresAction,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };

    /// <summary>
    ///     Maps a refund reason string to Stripe's accepted reason values.
    /// </summary>
    public static string? MapRefundReason(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
            return null;

        return reason.ToLowerInvariant() switch
        {
            "duplicate" => "duplicate",
            "fraudulent" => "fraudulent",
            "requested_by_customer" or "customer_request" => "requested_by_customer",
            _ => null
        };
    }
}

/// <summary>
///     Validates that payment requests carry Stripe object identifiers instead of raw card data.
/// </summary>
internal static class StripePaymentMethodIdentifier
{
    private static readonly Regex PaymentMethodIdPattern =
        new("^pm_[A-Za-z0-9_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const string ValidationMessage =
        "PaymentMethodId must be a Stripe payment method ID starting with 'pm_'. Raw card numbers are not accepted.";

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && PaymentMethodIdPattern.IsMatch(value.Trim());
}

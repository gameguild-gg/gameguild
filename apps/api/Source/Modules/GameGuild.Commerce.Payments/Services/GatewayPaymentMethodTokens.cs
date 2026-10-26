using System.Text.RegularExpressions;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Per-gateway payment-method token strategies (issue #413): classifies a payment method
///     identifier as a token of one of the registered gateways and validates its format.
///     Raw card data never validates. The classifier is deliberately conservative: an
///     identifier that matches no gateway's token grammar is rejected (fail closed) instead
///     of being attributed to the primary gateway.
/// </summary>
public static partial class GatewayPaymentMethodTokens
{
    /// <summary>Stripe payment method identifiers: <c>pm_...</c> (the historical rule).</summary>
    [GeneratedRegex("^pm_[A-Za-z0-9_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StripePattern();

    /// <summary>PayPal billing agreement / vault token identifiers.</summary>
    [GeneratedRegex("^(ba_|B-|PAYID-)[A-Za-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PayPalPattern();

    /// <summary>
    ///     Authorize.Net opaque data tokens: the Accept.js descriptor family joined with the
    ///     opaque value through a pipe by this module's adapter convention.
    /// </summary>
    [GeneratedRegex(
        "^COMMON\\.ACCEPT\\.[A-Za-z0-9_.]+(\\|[A-Za-z0-9_$.-]+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AuthorizeNetPattern();

    /// <summary>Braintree payment method nonces: opaque alphanumeric tokens of at least 8 characters.</summary>
    [GeneratedRegex("^[A-Za-z0-9_-]{8,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BraintreePattern();

    /// <summary>Adyen stored payment method ids (recurring detail references).</summary>
    [GeneratedRegex("^[A-Za-z0-9]{8,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AdyenPattern();

    /// <summary>Validation message surfaced by the API for tokens of no registered gateway.</summary>
    public const string ValidationMessage =
        "PaymentMethodId must be a payment method token of a registered gateway (Stripe pm_..., PayPal ba_.../B-..., Authorize.Net opaque data, a Braintree nonce or an Adyen stored payment method id). Raw card numbers are not accepted.";

    /// <summary>
    ///     Resolves the gateway that owns the token format, or null when the identifier
    ///     matches no registered gateway's token grammar (fail closed).
    /// </summary>
    public static string? ResolveProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var token = value.Trim();

        if (StripePattern().IsMatch(token))
        {
            return GatewayProviderIds.Stripe;
        }

        if (PayPalPattern().IsMatch(token))
        {
            return GatewayProviderIds.PayPal;
        }

        if (AuthorizeNetPattern().IsMatch(token))
        {
            return GatewayProviderIds.AuthorizeNet;
        }

        // Braintree nonces and Adyen stored ids are opaque strings; the dot/underscore
        // variants are Braintree's nonce alphabet, the plain alphanumeric form Adyen's.
        if (BraintreePattern().IsMatch(token))
        {
            return token.Contains('_') || token.Contains('-')
                ? GatewayProviderIds.Braintree
                : GatewayProviderIds.Adyen;
        }

        return null;
    }

    /// <summary>Whether the identifier is a valid token of any registered gateway.</summary>
    public static bool IsKnownGatewayToken(string? value) => ResolveProvider(value) is not null;
}

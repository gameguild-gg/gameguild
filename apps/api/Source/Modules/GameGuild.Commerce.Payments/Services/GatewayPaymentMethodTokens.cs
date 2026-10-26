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
        "^COMMON\\.ACCEPT\\.[A-Za-z0-9_.]+\\|[A-Za-z0-9_$.-]+$",
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
        "PaymentMethodId must be a gateway token. Opaque Braintree and Adyen tokens require an explicit GatewayProvider. Raw card numbers are not accepted.";

    /// <summary>
    ///     Resolves the gateway that owns the token format, or null when the identifier
    ///     matches no registered gateway's token grammar (fail closed).
    /// </summary>
    public static string? ResolveProvider(string? value, string? gatewayProvider = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var token = value.Trim();
        var provider = gatewayProvider?.Trim().ToLowerInvariant();
        if (token.Length > 512 || token.All(character => char.IsAsciiDigit(character) || character is ' ' or '-'))
        {
            return null;
        }

        if (StripePattern().IsMatch(token))
        {
            return provider is null or GatewayProviderIds.Stripe ? GatewayProviderIds.Stripe : null;
        }

        if (PayPalPattern().IsMatch(token))
        {
            return provider is null or GatewayProviderIds.PayPal ? GatewayProviderIds.PayPal : null;
        }

        if (AuthorizeNetPattern().IsMatch(token))
        {
            return provider is null or GatewayProviderIds.AuthorizeNet ? GatewayProviderIds.AuthorizeNet : null;
        }

        // Opaque token alphabets overlap. Never infer their owner from punctuation.
        return provider switch
        {
            GatewayProviderIds.Braintree when BraintreePattern().IsMatch(token) => GatewayProviderIds.Braintree,
            GatewayProviderIds.Adyen when AdyenPattern().IsMatch(token) => GatewayProviderIds.Adyen,
            _ => null
        };
    }

    /// <summary>Whether the identifier is a valid token of any registered gateway.</summary>
    public static bool IsKnownGatewayToken(string? value, string? gatewayProvider = null) => ResolveProvider(value, gatewayProvider) is not null;
}

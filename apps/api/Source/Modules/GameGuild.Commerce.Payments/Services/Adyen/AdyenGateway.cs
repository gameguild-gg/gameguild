using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Configuration for the Adyen processing adapter (issue #413, international gateway),
///     bound from <c>PaymentGateways:Adyen</c>. Fail-closed by default: the adapter is
///     disabled until explicitly enabled, defaults to the test endpoint, and enabling real
///     API calls without an API key and merchant account fails startup validation.
/// </summary>
public sealed class AdyenGatewayOptions : IGatewayHttpOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PaymentGateways:Adyen";

    /// <summary>Whether the adapter participates in gateway routing. Default false (fail-closed).</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Whether to return deterministic simulated responses instead of calling the API.</summary>
    public bool UseSimulation { get; set; }

    /// <summary>Adyen environment: <c>test</c> (default) or <c>live</c>.</summary>
    [Required]
    public string Environment { get; set; } = "test";

    /// <summary>Adyen API key (X-API-Key); required for real API calls.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Adyen merchant account name; required for real API calls.</summary>
    public string MerchantAccount { get; set; } = string.Empty;

    /// <summary>
    ///     Optional live URL prefix (Adyen live endpoints require the account-specific
    ///     prefix, for example <c>https://{prefix}-checkout-live.adyen.com</c>); when set it
    ///     overrides the derived live base URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>Per-request timeout, in seconds.</summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     Resolves the Checkout API base URL: the explicit override when set, otherwise the
    ///     test/live endpoint. Unknown environments resolve to the test endpoint
    ///     (fail-closed to the safer side).
    /// </summary>
    public string ResolveBaseUrl() =>
        !string.IsNullOrWhiteSpace(ApiBaseUrl)
            ? ApiBaseUrl.TrimEnd('/')
            : string.Equals(Environment, "live", StringComparison.OrdinalIgnoreCase)
                ? "https://checkout-live.adyen.com"
                : "https://checkout-test.adyen.com";

    /// <summary>Environment label recorded on provider mappings ("test" or "live").</summary>
    public string EnvironmentLabel =>
        string.Equals(Environment, "live", StringComparison.OrdinalIgnoreCase) ? "live" : "test";
}

/// <summary>
///     Startup validation for <see cref="AdyenGatewayOptions" />: enabling the adapter for
///     real API calls without credentials (or with an invalid environment or base URL) fails
///     fast instead of surfacing at the first charge.
/// </summary>
public sealed class AdyenGatewayOptionsValidator : IValidateOptions<AdyenGatewayOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AdyenGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!string.Equals(options.Environment, "test", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Environment, "live", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{nameof(AdyenGatewayOptions.Environment)} must be 'test' or 'live'.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > 300)
        {
            failures.Add($"{nameof(AdyenGatewayOptions.RequestTimeoutSeconds)} must be between 1 and 300 seconds.");
        }

        if (options.IsEnabled && !options.UseSimulation)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                failures.Add(
                    $"{nameof(AdyenGatewayOptions.ApiKey)} is required when Adyen is enabled without simulation.");
            }

            if (string.IsNullOrWhiteSpace(options.MerchantAccount))
            {
                failures.Add(
                    $"{nameof(AdyenGatewayOptions.MerchantAccount)} is required when Adyen is enabled without simulation.");
            }
        }

        AuthorizeNetGatewayOptionsValidator.ValidateApiBaseUrl(options, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
///     Thin HTTP client over the Adyen Checkout API (issue #413): X-API-Key authentication,
///     JSON bodies, amounts in minor currency units. All failures come back as
///     <see cref="GatewayProviderApiResult" /> values — the client never throws for transport
///     or HTTP failures, so callers fail closed on values, not exceptions.
/// </summary>
public interface IAdyenApiClient
{
    /// <summary>Authorises (and captures per the merchant account settings) a payment for a stored payment method.</summary>
    Task<GatewayProviderApiResult> PaymentAsync(
        string reference,
        decimal amount,
        string currency,
        string storedPaymentMethodId,
        CancellationToken cancellationToken = default);

    /// <summary>Refunds a captured payment, fully or partially.</summary>
    Task<GatewayProviderApiResult> RefundAsync(
        string pspReference,
        decimal? amount,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels an authorisation that has not been captured yet.</summary>
    Task<GatewayProviderApiResult> CancelAsync(
        string pspReference,
        CancellationToken cancellationToken = default);

    /// <summary>Cheap credential-validating call (available payment methods lookup).</summary>
    Task<GatewayProviderApiResult> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Default <see cref="IAdyenApiClient" /> over <see cref="HttpClient" /> (typed client
///     registered through <c>IHttpClientFactory</c>). Credentials are resolved per call
///     (encrypted credential store first, configuration fallback second).
/// </summary>
public sealed class AdyenApiClient(
    HttpClient httpClient,
    IOptions<AdyenGatewayOptions> options,
    IGatewayCredentialResolver credentialResolver,
    ILogger<AdyenApiClient> logger) : IAdyenApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly AdyenGatewayOptions _options = options.Value;
    private readonly IGatewayCredentialResolver _credentialResolver = credentialResolver;
    private readonly ILogger<AdyenApiClient> _logger = logger;

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> PaymentAsync(
        string reference,
        decimal amount,
        string currency,
        string storedPaymentMethodId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedPaymentMethodId);

        return SendAsync(
            HttpMethod.Post,
            "/payments",
            JsonSerializer.Serialize(new
            {
                merchantAccount = ResolveMerchantAccount(),
                reference,
                amount = new
                {
                    value = AdyenAmountConverter.ToMinorUnits(amount, currency),
                    currency = currency.ToUpperInvariant()
                },
                paymentMethod = new
                {
                    type = "scheme",
                    storedPaymentMethodId
                },
                recurringProcessingModel = "CardOnFile",
                shopperInteraction = "ContAuth"
            }, JsonOptions),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> RefundAsync(
        string pspReference,
        decimal? amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pspReference);

        object payload = amount.HasValue
            ? new
            {
                merchantAccount = ResolveMerchantAccount(),
                amount = new
                {
                    value = AdyenAmountConverter.ToMinorUnits(amount.Value, currency ?? "USD"),
                    currency = (currency ?? "USD").ToUpperInvariant()
                }
            }
            : new { merchantAccount = ResolveMerchantAccount() };

        return SendAsync(
            HttpMethod.Post,
            $"/payments/{Uri.EscapeDataString(pspReference)}/refunds",
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> CancelAsync(
        string pspReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pspReference);

        return SendAsync(
            HttpMethod.Post,
            $"/payments/{Uri.EscapeDataString(pspReference)}/cancels",
            JsonSerializer.Serialize(new { merchantAccount = ResolveMerchantAccount() }, JsonOptions),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> GetPaymentMethodsAsync(CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Post,
            "/paymentMethods",
            JsonSerializer.Serialize(new
            {
                merchantAccount = ResolveMerchantAccount(),
                amount = new { value = 1000, currency = "USD" }
            }, JsonOptions),
            cancellationToken);

    private string ResolveMerchantAccount() =>
        _credentialResolver
            .ResolveAsync(GatewayProviderIds.Adyen, "merchant_account", _options.MerchantAccount, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult() ?? _options.MerchantAccount;

    private async Task<GatewayProviderApiResult> SendAsync(
        HttpMethod method,
        string path,
        string? content,
        CancellationToken cancellationToken)
    {
        var apiKey = await _credentialResolver
            .ResolveAsync(GatewayProviderIds.Adyen, "api_key", _options.ApiKey, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GatewayProviderApiResult(
                Success: false,
                StatusCode: null,
                Body: null,
                ErrorCode: "adyen_credentials_missing",
                ErrorMessage:
                    "Adyen credentials are incomplete: provision encrypted credentials (adyen/api_key, adyen/merchant_account) or set PaymentGateways:Adyen:ApiKey/MerchantAccount.",
                TransportFailed: false);
        }

        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-API-Key", apiKey);
            if (content is not null)
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new GatewayProviderApiResult(true, (int)response.StatusCode, body, null, null, TransportFailed: false);
            }

            var (errorCode, errorMessage) = ParseErrorBody(body);
            _logger.LogWarning(
                "Adyen call to {Path} failed with {StatusCode}: {ErrorCode} - {ErrorMessage}",
                path,
                (int)response.StatusCode,
                errorCode,
                errorMessage);
            return new GatewayProviderApiResult(
                false,
                (int)response.StatusCode,
                body,
                errorCode,
                errorMessage,
                TransportFailed: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogWarning(exception, "Adyen call to {Path} never produced a response", path);
            return new GatewayProviderApiResult(
                false,
                null,
                null,
                "adyen_transport_failure",
                exception.Message,
                TransportFailed: true);
        }
    }

    private static (string? Code, string? Message) ParseErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var status = root.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Number
                ? statusElement.GetInt32()
                : (int?)null;
            var errorCode = root.TryGetProperty("errorCode", out var codeElement) && codeElement.ValueKind == JsonValueKind.String
                ? codeElement.GetString()
                : null;
            var message = root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String
                ? messageElement.GetString()
                : null;

            return (errorCode ?? (status is { } httpStatus ? $"adyen_http_{httpStatus}" : null), message);
        }
        catch (JsonException)
        {
            return (null, body.Length > 200 ? body[..200] : body);
        }
    }
}

/// <summary>
///     Converts major currency units to Adyen minor units (ISO 4217 exponent): x100 for
///     two-decimal currencies, no conversion for zero-decimal currencies (JPY, KRW, ...).
/// </summary>
public static class AdyenAmountConverter
{
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "JPY",
        "KRW",
        "VND",
        "CLP",
        "ISK"
    };

    /// <summary>Converts a major-unit amount into Adyen minor units for the currency.</summary>
    public static long ToMinorUnits(decimal amount, string currency) =>
        ZeroDecimalCurrencies.Contains(currency)
            ? (long)decimal.Truncate(amount)
            : (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}

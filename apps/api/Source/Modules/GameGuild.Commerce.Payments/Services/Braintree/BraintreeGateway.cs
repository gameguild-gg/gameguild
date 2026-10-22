using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Configuration for the Braintree processing adapter (issue #413), bound from
///     <c>PaymentGateways:Braintree</c>. Fail-closed by default: the adapter is disabled
///     until explicitly enabled, defaults to the sandbox endpoint, and enabling real API
///     calls without an API key pair fails startup validation.
/// </summary>
public sealed class BraintreeGatewayOptions : IGatewayHttpOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PaymentGateways:Braintree";

    /// <summary>Whether the adapter participates in gateway routing. Default false (fail-closed).</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Whether to return deterministic simulated responses instead of calling the API.</summary>
    public bool UseSimulation { get; set; }

    /// <summary>Braintree environment: <c>sandbox</c> (default) or <c>production</c>.</summary>
    [Required]
    public string Environment { get; set; } = "sandbox";

    /// <summary>Braintree merchant id; required for real API calls.</summary>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>Braintree API key (public key of the API key pair); required for real API calls.</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Braintree API secret (private key of the API key pair); required for real API calls.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>Optional explicit API base URL (for tests or proxies); empty derives it from the environment.</summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>Per-request timeout, in seconds.</summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     Resolves the API base URL: the explicit override when set, otherwise the
    ///     sandbox/production endpoint. Unknown environments resolve to the sandbox endpoint
    ///     (fail-closed to the safer side).
    /// </summary>
    public string ResolveBaseUrl() =>
        !string.IsNullOrWhiteSpace(ApiBaseUrl)
            ? ApiBaseUrl.TrimEnd('/')
            : string.Equals(Environment, "production", StringComparison.OrdinalIgnoreCase)
                ? "https://api.braintreegateway.com"
                : "https://api.sandbox.braintreegateway.com";

    /// <summary>Environment label recorded on provider mappings ("sandbox" or "production").</summary>
    public string EnvironmentLabel =>
        string.Equals(Environment, "production", StringComparison.OrdinalIgnoreCase) ? "production" : "sandbox";
}

/// <summary>
///     Startup validation for <see cref="BraintreeGatewayOptions" />: enabling the adapter for
///     real API calls without an API key pair (or with an invalid environment or base URL)
///     fails fast instead of surfacing at the first charge.
/// </summary>
public sealed class BraintreeGatewayOptionsValidator : IValidateOptions<BraintreeGatewayOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BraintreeGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!string.Equals(options.Environment, "sandbox", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Environment, "production", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{nameof(BraintreeGatewayOptions.Environment)} must be 'sandbox' or 'production'.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > 300)
        {
            failures.Add($"{nameof(BraintreeGatewayOptions.RequestTimeoutSeconds)} must be between 1 and 300 seconds.");
        }

        if (options.IsEnabled && !options.UseSimulation)
        {
            if (string.IsNullOrWhiteSpace(options.MerchantId))
            {
                failures.Add(
                    $"{nameof(BraintreeGatewayOptions.MerchantId)} is required when Braintree is enabled without simulation.");
            }

            if (string.IsNullOrWhiteSpace(options.PublicKey))
            {
                failures.Add(
                    $"{nameof(BraintreeGatewayOptions.PublicKey)} is required when Braintree is enabled without simulation.");
            }

            if (string.IsNullOrWhiteSpace(options.PrivateKey))
            {
                failures.Add(
                    $"{nameof(BraintreeGatewayOptions.PrivateKey)} is required when Braintree is enabled without simulation.");
            }
        }

        AuthorizeNetGatewayOptionsValidator.ValidateApiBaseUrl(options, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
///     Thin HTTP client over the Braintree server-to-server REST API (issue #413): Basic
///     authentication with the API key pair, form-encoded request bodies and XML response
///     bodies (Braintree's documented wire format for server SDKs). All failures come back as
///     <see cref="GatewayProviderApiResult" /> values — the client never throws for transport
///     or HTTP failures, so callers fail closed on values, not exceptions.
/// </summary>
public interface IBraintreeApiClient
{
    /// <summary>Charges a payment method nonce and submits the transaction for settlement (sale).</summary>
    Task<GatewayProviderApiResult> SaleAsync(
        string idempotencyKey,
        decimal amount,
        string paymentMethodNonce,
        string? orderId,
        CancellationToken cancellationToken = default);

    /// <summary>Refunds a settled transaction, fully or partially.</summary>
    Task<GatewayProviderApiResult> RefundAsync(
        string transactionId,
        decimal? amount,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the current state of a transaction.</summary>
    Task<GatewayProviderApiResult> GetTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>Cheap credential-validating call (client-token creation).</summary>
    Task<GatewayProviderApiResult> CreateClientTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Default <see cref="IBraintreeApiClient" /> over <see cref="HttpClient" /> (typed client
///     registered through <c>IHttpClientFactory</c>). Credentials are resolved per call
///     (encrypted credential store first, configuration fallback second).
/// </summary>
public sealed class BraintreeApiClient(
    HttpClient httpClient,
    IOptions<BraintreeGatewayOptions> options,
    IGatewayCredentialResolver credentialResolver,
    ILogger<BraintreeApiClient> logger) : IBraintreeApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly BraintreeGatewayOptions _options = options.Value;
    private readonly IGatewayCredentialResolver _credentialResolver = credentialResolver;
    private readonly ILogger<BraintreeApiClient> _logger = logger;

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> SaleAsync(
        string idempotencyKey,
        decimal amount,
        string paymentMethodNonce,
        string? orderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethodNonce);

        return SendAsync(
            $"merchants/{Uri.EscapeDataString(ResolveMerchantId())}/transactions",
            new Dictionary<string, string>
            {
                ["transaction[amount]"] = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                ["transaction[payment_method_nonce]"] = paymentMethodNonce,
                ["transaction[order_id]"] = orderId ?? idempotencyKey,
                ["transaction[options][submit_for_settlement]"] = "true"
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> RefundAsync(
        string transactionId,
        decimal? amount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        var form = new Dictionary<string, string>();
        if (amount.HasValue)
        {
            form["amount"] = amount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        return SendAsync(
            $"merchants/{Uri.EscapeDataString(ResolveMerchantId())}/transactions/{Uri.EscapeDataString(transactionId)}/refund",
            form,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> GetTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        return SendFormlessGetAsync(
            $"merchants/{Uri.EscapeDataString(ResolveMerchantId())}/transactions/{Uri.EscapeDataString(transactionId)}",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> CreateClientTokenAsync(CancellationToken cancellationToken = default) =>
        SendAsync(
            $"merchants/{Uri.EscapeDataString(ResolveMerchantId())}/client_token",
            new Dictionary<string, string> { ["client_token[version]"] = "2" },
            cancellationToken);

    private string ResolveMerchantId() =>
        _credentialResolver
            .ResolveAsync(GatewayProviderIds.Braintree, "merchant_id", _options.MerchantId, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult() ?? _options.MerchantId;

    private async Task<GatewayProviderApiResult> SendAsync(
        string path,
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        var (publicKey, privateKey) = await ResolveKeyPairAsync(cancellationToken).ConfigureAwait(false);
        if (publicKey is null || privateKey is null)
        {
            return CredentialsMissing();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new FormUrlEncodedContent(form)
            };
            ApplyAuthentication(request, publicKey, privateKey);

            return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return TransportFailure(exception);
        }
    }

    private async Task<GatewayProviderApiResult> SendFormlessGetAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var (publicKey, privateKey) = await ResolveKeyPairAsync(cancellationToken).ConfigureAwait(false);
        if (publicKey is null || privateKey is null)
        {
            return CredentialsMissing();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            ApplyAuthentication(request, publicKey, privateKey);

            return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return TransportFailure(exception);
        }
    }

    private async Task<GatewayProviderApiResult> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return new GatewayProviderApiResult(true, (int)response.StatusCode, body, null, null, TransportFailed: false);
        }

        var (errorCode, errorMessage) = ParseXmlError(body);
        _logger.LogWarning(
            "Braintree call failed with {StatusCode}: {ErrorCode} - {ErrorMessage}",
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

    private async Task<(string? PublicKey, string? PrivateKey)> ResolveKeyPairAsync(CancellationToken cancellationToken)
    {
        var publicKey = await _credentialResolver
            .ResolveAsync(GatewayProviderIds.Braintree, "public_key", _options.PublicKey, cancellationToken)
            .ConfigureAwait(false);
        var privateKey = await _credentialResolver
            .ResolveAsync(GatewayProviderIds.Braintree, "private_key", _options.PrivateKey, cancellationToken)
            .ConfigureAwait(false);

        return (publicKey, privateKey);
    }

    private GatewayProviderApiResult CredentialsMissing() =>
        new(
            Success: false,
            StatusCode: null,
            Body: null,
            ErrorCode: "braintree_credentials_missing",
            ErrorMessage:
                "Braintree credentials are incomplete: provision encrypted credentials (braintree/merchant_id, braintree/public_key, braintree/private_key) or set PaymentGateways:Braintree.",
            TransportFailed: false);

    private GatewayProviderApiResult TransportFailure(Exception exception)
    {
        _logger.LogWarning(exception, "Braintree call never produced a response");
        return new GatewayProviderApiResult(
            false,
            null,
            null,
            "braintree_transport_failure",
            exception.Message,
            TransportFailed: true);
    }

    private static void ApplyAuthentication(HttpRequestMessage request, string publicKey, string privateKey)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publicKey}:{privateKey}")));
        request.Headers.Accept.ParseAdd("application/xml");
    }

    private static (string? Code, string? Message) ParseXmlError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null);
        }

        try
        {
            var document = XDocument.Parse(body);
            var error = document.Descendants("error").FirstOrDefault();
            if (error is not null)
            {
                var code = (string?)error.Attribute("code");
                var message = (string?)error.Element("message");
                return (code, message);
            }

            var messageElement = document.Descendants("message").FirstOrDefault();
            return messageElement is null ? (null, null) : (null, messageElement.Value);
        }
        catch (System.Xml.XmlException)
        {
            return (null, body.Length > 200 ? body[..200] : body);
        }
    }
}

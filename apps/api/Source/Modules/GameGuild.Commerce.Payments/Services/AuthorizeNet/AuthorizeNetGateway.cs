using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Configuration for the Authorize.Net processing adapter (issue #413), bound from
///     <c>PaymentGateways:AuthorizeNet</c>. Fail-closed by default: the adapter is disabled
///     until explicitly enabled, defaults to the sandbox endpoint, and enabling real API
///     calls without credentials fails startup validation.
/// </summary>
public sealed class AuthorizeNetGatewayOptions : IGatewayHttpOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PaymentGateways:AuthorizeNet";

    /// <summary>Whether the adapter participates in gateway routing. Default false (fail-closed).</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Whether to return deterministic simulated responses instead of calling the API.</summary>
    public bool UseSimulation { get; set; }

    /// <summary>Authorize.Net environment: <c>sandbox</c> (default) or <c>live</c>.</summary>
    [Required]
    public string Environment { get; set; } = "sandbox";

    /// <summary>Authorize.Net API login id; required for real API calls.</summary>
    public string ApiLoginId { get; set; } = string.Empty;

    /// <summary>Authorize.Net transaction key; required for real API calls.</summary>
    public string TransactionKey { get; set; } = string.Empty;

    /// <summary>Optional explicit API base URL (for tests or proxies); empty derives it from the environment.</summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>Per-request timeout, in seconds.</summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     Resolves the API base URL: the explicit override when set, otherwise the
    ///     sandbox/live endpoint. Unknown environments resolve to the sandbox endpoint
    ///     (fail-closed to the safer side).
    /// </summary>
    public string ResolveBaseUrl() =>
        !string.IsNullOrWhiteSpace(ApiBaseUrl)
            ? ApiBaseUrl.TrimEnd('/')
            : string.Equals(Environment, "live", StringComparison.OrdinalIgnoreCase)
                ? "https://api.anetgateway.com"
                : "https://api-sandbox.anetgateway.com";

    /// <summary>Environment label recorded on provider mappings ("sandbox" or "live").</summary>
    public string EnvironmentLabel =>
        string.Equals(Environment, "live", StringComparison.OrdinalIgnoreCase) ? "live" : "sandbox";
}

/// <summary>
///     Startup validation for <see cref="AuthorizeNetGatewayOptions" />: enabling the adapter
///     for real API calls without credentials (or with an invalid environment or base URL)
///     fails fast instead of surfacing at the first charge.
/// </summary>
public sealed class AuthorizeNetGatewayOptionsValidator : IValidateOptions<AuthorizeNetGatewayOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AuthorizeNetGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!string.Equals(options.Environment, "sandbox", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Environment, "live", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{nameof(AuthorizeNetGatewayOptions.Environment)} must be 'sandbox' or 'live'.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > 300)
        {
            failures.Add($"{nameof(AuthorizeNetGatewayOptions.RequestTimeoutSeconds)} must be between 1 and 300 seconds.");
        }

        if (options.IsEnabled && !options.UseSimulation)
        {
            if (string.IsNullOrWhiteSpace(options.ApiLoginId))
            {
                failures.Add(
                    $"{nameof(AuthorizeNetGatewayOptions.ApiLoginId)} is required when Authorize.Net is enabled without simulation.");
            }

            if (string.IsNullOrWhiteSpace(options.TransactionKey))
            {
                failures.Add(
                    $"{nameof(AuthorizeNetGatewayOptions.TransactionKey)} is required when Authorize.Net is enabled without simulation.");
            }
        }

        ValidateApiBaseUrl(options, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    internal static void ValidateApiBaseUrl<TOptions>(TOptions options, List<string> failures)
        where TOptions : class, IGatewayHttpOptions
    {
        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
        {
            return;
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var baseUri))
        {
            failures.Add("ApiBaseUrl must be an absolute URL when set.");
        }
        else if (baseUri.Scheme is not "http" and not "https")
        {
            failures.Add("ApiBaseUrl must use http or https.");
        }
    }
}

/// <summary>Options contract shared by the gateway HTTP-option validators.</summary>
public interface IGatewayHttpOptions
{
    /// <summary>Optional explicit API base URL override.</summary>
    string ApiBaseUrl { get; }
}

/// <summary>
///     Thin HTTP client over the Authorize.Net JSON API (issue #413). Authorize.Net multiplexes
///     every operation through one endpoint (<c>/xml/v1/transact.api</c>) with the operation
///     named inside the JSON body and the merchant authentication carried per request. All
///     failures come back as <see cref="GatewayProviderApiResult" /> values — the client never
///     throws for transport or HTTP failures, so callers fail closed on values, not exceptions.
/// </summary>
public interface IAuthorizeNetApiClient
{
    /// <summary>Charges an amount to a stored payment token (opaque data from Accept.js) and captures it.</summary>
    Task<GatewayProviderApiResult> ChargeAsync(
        string refId,
        decimal amount,
        string currency,
        string paymentToken,
        string? description,
        CancellationToken cancellationToken = default);

    /// <summary>Refunds a previously captured transaction, fully or partially.</summary>
    Task<GatewayProviderApiResult> RefundAsync(
        string refId,
        string transactionId,
        decimal? amount,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the current state of a transaction.</summary>
    Task<GatewayProviderApiResult> GetTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>Cheap credential-validating call (<c>getMerchantDetailsRequest</c>).</summary>
    Task<GatewayProviderApiResult> GetMerchantDetailsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Default <see cref="IAuthorizeNetApiClient" /> over <see cref="HttpClient" /> (typed
///     client registered through <c>IHttpClientFactory</c>). Credentials are resolved per
///     call (encrypted credential store first, configuration fallback second) so provisioning
///     a rotated key takes effect without a process restart.
/// </summary>
public sealed class AuthorizeNetApiClient(
    HttpClient httpClient,
    IOptions<AuthorizeNetGatewayOptions> options,
    IGatewayCredentialResolver credentialResolver,
    ILogger<AuthorizeNetApiClient> logger) : IAuthorizeNetApiClient
{
    private const string TransactPath = "/xml/v1/transact.api";

    private readonly HttpClient _httpClient = httpClient;
    private readonly AuthorizeNetGatewayOptions _options = options.Value;
    private readonly IGatewayCredentialResolver _credentialResolver = credentialResolver;
    private readonly ILogger<AuthorizeNetApiClient> _logger = logger;

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> ChargeAsync(
        string refId,
        decimal amount,
        string currency,
        string paymentToken,
        string? description,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentToken);

        // Authorize.Net charges stored Accept.js opaque data tokens; the descriptor/value
        // pair is joined with '|' in the payment method id by convention of this adapter.
        var parts = paymentToken.Split('|', 2);
        var dataDescriptor = parts[0];
        var dataValue = parts.Length > 1 ? parts[1] : paymentToken;

        return SendAsync(
            new
            {
                createTransactionRequest = new
                {
                    refId,
                    transactionRequest = new
                    {
                        transactionType = "authCaptureTransaction",
                        amount = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        currencyCode = currency.ToUpperInvariant(),
                        payment = new { opaqueData = new { dataDescriptor, dataValue } },
                        order = new { description }
                    }
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> RefundAsync(
        string refId,
        string transactionId,
        decimal? amount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        return SendAsync(
            new
            {
                createTransactionRequest = new
                {
                    refId,
                    transactionRequest = new
                    {
                        transactionType = "refundTransaction",
                        amount = (amount is null
                            ? 0m // Unspecified refund amount: Authorize.Net requires the field; 0 triggers a full refund.
                            : amount.Value).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        refTransId = transactionId
                    }
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> GetTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        return SendAsync(
            new { getTransactionDetailsRequest = new { transId = transactionId } },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<GatewayProviderApiResult> GetMerchantDetailsAsync(CancellationToken cancellationToken = default) =>
        SendAsync(new { getMerchantDetailsRequest = new { } }, cancellationToken);

    private async Task<GatewayProviderApiResult> SendAsync(object payload, CancellationToken cancellationToken)
    {
        var apiLoginId = await _credentialResolver
            .ResolveAsync(GatewayProviderIds.AuthorizeNet, "api_login_id", _options.ApiLoginId, cancellationToken)
            .ConfigureAwait(false);
        var transactionKey = await _credentialResolver
            .ResolveAsync(GatewayProviderIds.AuthorizeNet, "transaction_key", _options.TransactionKey, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(apiLoginId) || string.IsNullOrWhiteSpace(transactionKey))
        {
            return new GatewayProviderApiResult(
                Success: false,
                StatusCode: null,
                Body: null,
                ErrorCode: "authorizenet_credentials_missing",
                ErrorMessage:
                    "Authorize.Net credentials are incomplete: provision encrypted credentials (authorizenet/api_login_id, authorizenet/transaction_key) or set PaymentGateways:AuthorizeNet:ApiLoginId/TransactionKey.",
                TransportFailed: false);
        }

        try
        {
            // Authorize.Net multiplexes operations through one endpoint; the merchant
            // authentication envelope is merged into the operation payload per call.
            using var payloadDocument = JsonSerializer.SerializeToDocument(payload, JsonSerializerOptions);
            var request = new JsonObject();
            foreach (var property in payloadDocument.RootElement.EnumerateObject())
            {
                request[property.Name] = JsonNode.Parse(property.Value.GetRawText());
            }

            request["merchantAuthentication"] = new JsonObject
            {
                ["name"] = apiLoginId,
                ["transactionKey"] = transactionKey
            };

            var body = request.ToJsonString(JsonSerializerOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, TransactPath)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new GatewayProviderApiResult(true, (int)response.StatusCode, responseBody, null, null, TransportFailed: false);
            }

            var (errorCode, errorMessage) = ParseErrorBody(responseBody);
            _logger.LogWarning(
                "Authorize.Net call failed with {StatusCode}: {ErrorCode} - {ErrorMessage}",
                (int)response.StatusCode,
                errorCode,
                errorMessage);
            return new GatewayProviderApiResult(
                false,
                (int)response.StatusCode,
                responseBody,
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
            _logger.LogWarning(exception, "Authorize.Net call never produced a response");
            return new GatewayProviderApiResult(
                false,
                null,
                null,
                "authorizenet_transport_failure",
                exception.Message,
                TransportFailed: true);
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

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

            if (root.TryGetProperty("messages", out var messages))
            {
                var resultCode = messages.TryGetProperty("resultCode", out var code) && code.ValueKind == JsonValueKind.String
                    ? code.GetString()
                    : null;

                if (messages.TryGetProperty("message", out var messageList) &&
                    messageList.ValueKind == JsonValueKind.Array &&
                    messageList.GetArrayLength() > 0)
                {
                    var first = messageList[0];
                    var messageCode = first.TryGetProperty("code", out var mc) && mc.ValueKind == JsonValueKind.String
                        ? mc.GetString()
                        : null;
                    var text = first.TryGetProperty("text", out var mt) && mt.ValueKind == JsonValueKind.String
                        ? mt.GetString()
                        : null;

                    return (messageCode ?? resultCode, text);
                }

                return (resultCode, null);
            }

            return (null, body.Length > 200 ? body[..200] : body);
        }
        catch (JsonException)
        {
            return (null, body.Length > 200 ? body[..200] : body);
        }
    }
}

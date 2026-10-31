using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Stripe;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Connection test for the Stripe adapter (issue #413): resolves the effective secret key
///     (encrypted credential store first, <c>PaymentGateways:Stripe:ApiKey</c> fallback) and
///     performs the cheapest authenticated Stripe call — <c>GET /v1/balance</c> — to prove the
///     credentials work. Simulation mode returns an explicit simulated pass without any HTTP
///     call; an unconfigured adapter fails closed.
/// </summary>
public sealed class StripeGatewayConnectionTester(
    IOptions<StripeGatewayOptions> options,
    IGatewayCredentialResolver credentialResolver) : IGatewayConnectionTester
{
    private readonly StripeGatewayOptions _options = options.Value;

    /// <inheritdoc />
    public string ProviderId => GatewayProviderIds.Stripe;

    /// <summary>
    ///     The cheap authenticated call used for the test (Stripe <c>GET /v1/balance</c>),
    ///     overridable for unit tests.
    /// </summary>
    internal Func<string, CancellationToken, Task> BalanceProbe { get; init; } =
        static (apiKey, cancellationToken) =>
        {
            StripeConfiguration.ApiKey = apiKey;
            return new BalanceService().GetAsync(cancellationToken: cancellationToken);
        };

    /// <inheritdoc />
    public async Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = SystemClock.UtcNow;

        if (!_options.IsEnabled)
        {
            return Failed("adapter_disabled", "The Stripe adapter is disabled; no credentials were exercised.", 0, checkedAtUtc);
        }

        if (_options.UseSimulation)
        {
            return new GatewayConnectionTestResult(
                ProviderId,
                Succeeded: true,
                Mode: "simulation",
                LatencyMs: 0,
                ErrorCode: null,
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc);
        }

        var apiKey = await credentialResolver
            .ResolveAsync(ProviderId, "api_key", _options.ApiKey, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Failed(
                "stripe_credentials_missing",
                "No Stripe API key is configured: provision an encrypted credential (stripe/api_key) or set PaymentGateways:Stripe:ApiKey.",
                0,
                checkedAtUtc);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await BalanceProbe(apiKey, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return new GatewayConnectionTestResult(
                ProviderId,
                Succeeded: true,
                Mode: "live",
                LatencyMs: stopwatch.ElapsedMilliseconds,
                ErrorCode: null,
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (StripeException exception)
        {
            stopwatch.Stop();
            return Failed(
                $"stripe_http_{(int)exception.HttpStatusCode}",
                $"Stripe rejected the credentials or the balance probe failed: {exception.StripeError?.Message ?? exception.Message}",
                stopwatch.ElapsedMilliseconds,
                checkedAtUtc);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            stopwatch.Stop();
            return Failed(
                "stripe_transport_failure",
                $"The Stripe balance probe never produced a response: {exception.Message}",
                stopwatch.ElapsedMilliseconds,
                checkedAtUtc);
        }
    }

    private GatewayConnectionTestResult Failed(
        string errorCode,
        string errorMessage,
        long latencyMs,
        DateTime checkedAtUtc) =>
        new(ProviderId, Succeeded: false, Mode: "live", LatencyMs: latencyMs, ErrorCode: errorCode, ErrorMessage: errorMessage, CheckedAtUtc: checkedAtUtc);
}

/// <summary>
///     Connection test for the PayPal adapter (issue #413): resolves the effective client
///     credentials (encrypted credential store first, <c>PaymentGateways:PayPal</c> fallback)
///     and performs the PayPal OAuth2 client-credentials token request — the cheapest
///     credential-validating call the PayPal REST API offers. Simulation mode returns an
///     explicit simulated pass without any HTTP call; an unconfigured adapter fails closed.
/// </summary>
public sealed class PayPalGatewayConnectionTester(
    IOptions<PayPalGatewayOptions> options,
    IGatewayCredentialResolver credentialResolver) : IGatewayConnectionTester
{
    /// <summary>Named HTTP client used for the OAuth2 token probe (see PaymentsModule).</summary>
    public const string HttpClientName = "paypal-connection-test";

    private readonly PayPalGatewayOptions _options = options.Value;
    private readonly IGatewayCredentialResolver _credentialResolver = credentialResolver;

    /// <summary>
    ///     The OAuth2 token probe: receives the resolved credentials and the probe HTTP client,
    ///     returns the response status code (null on transport failure). Overridable for unit
    ///     tests; the default performs the documented token request.
    /// </summary>
    internal Func<HttpClient, string, string, CancellationToken, Task<int?>> TokenProbe { get; init; } =
        static async (httpClient, clientId, clientSecret, cancellationToken) =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token")
            {
                Content = new StringContent(
                    "grant_type=client_credentials",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return (int)response.StatusCode;
        };

    /// <summary>
    ///     Creates the probe HTTP client, overridable for unit tests; the default builds one
    ///     bound to the adapter's resolved base URL and timeout.
    /// </summary>
    internal Func<HttpClient>? HttpClientFactory { get; init; }

    /// <inheritdoc />
    public string ProviderId => GatewayProviderIds.PayPal;

    /// <inheritdoc />
    public async Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = SystemClock.UtcNow;

        if (!_options.IsEnabled)
        {
            return Failed("adapter_disabled", "The PayPal adapter is disabled; no credentials were exercised.", 0, checkedAtUtc);
        }

        if (_options.UseSimulation)
        {
            return new GatewayConnectionTestResult(
                ProviderId,
                Succeeded: true,
                Mode: "simulation",
                LatencyMs: 0,
                ErrorCode: null,
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc);
        }

        var clientId = await _credentialResolver
            .ResolveAsync(ProviderId, "client_id", _options.ClientId, cancellationToken)
            .ConfigureAwait(false);
        var clientSecret = await _credentialResolver
            .ResolveAsync(ProviderId, "client_secret", _options.ClientSecret, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Failed(
                "paypal_credentials_missing",
                "PayPal client credentials are incomplete: provision encrypted credentials (paypal/client_id, paypal/client_secret) or set PaymentGateways:PayPal:ClientId/ClientSecret.",
                0,
                checkedAtUtc);
        }

        using var httpClient = CreateProbeClient();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            int? statusCode = await TokenProbe(httpClient, clientId, clientSecret, cancellationToken)
                .ConfigureAwait(false);
            stopwatch.Stop();

            return statusCode is >= 200 and < 300
                ? new GatewayConnectionTestResult(
                    ProviderId,
                    Succeeded: true,
                    Mode: "live",
                    LatencyMs: stopwatch.ElapsedMilliseconds,
                    ErrorCode: null,
                    ErrorMessage: null,
                    CheckedAtUtc: checkedAtUtc)
                : Failed(
                    $"paypal_http_{statusCode}",
                    $"PayPal rejected the OAuth2 token request with HTTP {statusCode} ({_options.EnvironmentLabel} environment).",
                    stopwatch.ElapsedMilliseconds,
                    checkedAtUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            stopwatch.Stop();
            return Failed(
                "paypal_transport_failure",
                $"The PayPal OAuth2 token request never produced a response: {exception.Message}",
                stopwatch.ElapsedMilliseconds,
                checkedAtUtc);
        }
    }

    private HttpClient CreateProbeClient() =>
        HttpClientFactory?.Invoke()
        ?? new HttpClient
        {
            BaseAddress = new Uri(_options.ResolveBaseUrl(), UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds))
        };

    private GatewayConnectionTestResult Failed(
        string errorCode,
        string errorMessage,
        long latencyMs,
        DateTime checkedAtUtc) =>
        new(ProviderId, Succeeded: false, Mode: "live", LatencyMs: latencyMs, ErrorCode: errorCode, ErrorMessage: errorMessage, CheckedAtUtc: checkedAtUtc);
}

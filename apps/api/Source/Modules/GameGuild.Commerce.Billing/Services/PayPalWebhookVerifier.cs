using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Configuration options for PayPal webhook verification.
/// </summary>
public class PayPalWebhookOptions
{
    public const string SectionName = "PayPal";

    /// <summary>
    ///     PayPal Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     PayPal Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    ///     PayPal Webhook ID (from PayPal developer dashboard)
    /// </summary>
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>
    ///     Whether to use sandbox environment
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    ///     PayPal API base URL
    /// </summary>
    public string BaseUrl => UseSandbox
        ? "https://api-m.sandbox.paypal.com"
        : "https://api-m.paypal.com";
}

/// <summary>
///     Interface for PayPal webhook signature verification.
/// </summary>
public interface IPayPalWebhookVerifier
{
    /// <summary>
    ///     Verifies a PayPal webhook signature using PayPal's verification API.
    /// </summary>
    /// <param name="transmissionId">PayPal transmission ID header</param>
    /// <param name="transmissionTime">PayPal transmission time header</param>
    /// <param name="transmissionSig">PayPal signature header</param>
    /// <param name="certUrl">PayPal certificate URL header</param>
    /// <param name="authAlgo">PayPal auth algorithm header</param>
    /// <param name="webhookEvent">Raw webhook payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<PayPalVerificationResult> VerifyWebhookSignatureAsync(
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string certUrl,
        string authAlgo,
        string webhookEvent,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of PayPal webhook verification.
/// </summary>
public class PayPalVerificationResult
{
    public bool IsValid { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public static PayPalVerificationResult Success() => new()
    {
        IsValid = true,
        VerificationStatus = "SUCCESS"
    };

    public static PayPalVerificationResult Failed(string error) => new()
    {
        IsValid = false,
        VerificationStatus = "FAILURE",
        ErrorMessage = error
    };
}

/// <summary>
///     PayPal webhook signature verifier using PayPal's API.
///     Implements verification per PayPal Webhooks documentation:
///     https://developer.paypal.com/docs/api/webhooks/v1/#verify-webhook-signature
/// </summary>
public class PayPalWebhookVerifier : IPayPalWebhookVerifier
{
    private readonly HttpClient _httpClient;
    private readonly PayPalWebhookOptions _options;
    private readonly ILogger<PayPalWebhookVerifier> _logger;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PayPalWebhookVerifier(
        HttpClient httpClient,
        IOptions<PayPalWebhookOptions> options,
        ILogger<PayPalWebhookVerifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PayPalVerificationResult> VerifyWebhookSignatureAsync(
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string certUrl,
        string authAlgo,
        string webhookEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate required parameters
            if (string.IsNullOrEmpty(transmissionId) ||
                string.IsNullOrEmpty(transmissionTime) ||
                string.IsNullOrEmpty(transmissionSig))
            {
                return PayPalVerificationResult.Failed("Missing required transmission headers");
            }

            // Get access token for API call
            var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(accessToken))
            {
                return PayPalVerificationResult.Failed("Failed to obtain PayPal access token");
            }

            // Build verification request
            var verificationRequest = new
            {
                auth_algo = authAlgo,
                cert_url = certUrl,
                transmission_id = transmissionId,
                transmission_sig = transmissionSig,
                transmission_time = transmissionTime,
                webhook_id = _options.WebhookId,
                webhook_event = System.Text.Json.JsonDocument.Parse(webhookEvent).RootElement
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_options.BaseUrl}/v1/notifications/verify-webhook-signature");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(verificationRequest);

            _logger.LogDebug("Sending webhook verification request to PayPal. TransmissionId={TransmissionId}", transmissionId);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("PayPal verification API returned error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return PayPalVerificationResult.Failed($"PayPal API error: {response.StatusCode}");
            }

            var verificationResponse = await response.Content.ReadFromJsonAsync<PayPalVerifyResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (verificationResponse?.VerificationStatus == "SUCCESS")
            {
                _logger.LogInformation("PayPal webhook signature verified successfully. TransmissionId={TransmissionId}",
                    transmissionId);
                return PayPalVerificationResult.Success();
            }

            _logger.LogWarning("PayPal webhook signature verification failed. Status={Status}, TransmissionId={TransmissionId}",
                verificationResponse?.VerificationStatus, transmissionId);
            return PayPalVerificationResult.Failed($"Verification status: {verificationResponse?.VerificationStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during PayPal webhook verification. TransmissionId={TransmissionId}",
                transmissionId);
            return PayPalVerificationResult.Failed(ex.Message);
        }
    }

    /// <summary>
    ///     Gets an access token from PayPal using client credentials.
    /// </summary>
    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedAccessToken;
            }

            // Request new token
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get PayPal access token: {StatusCode}", response.StatusCode);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<PayPalTokenResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (tokenResponse != null)
            {
                _cachedAccessToken = tokenResponse.AccessToken;
                // Expire token 60 seconds before actual expiry for safety
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
            }

            return _cachedAccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private class PayPalTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    private class PayPalVerifyResponse
    {
        public string VerificationStatus { get; set; } = string.Empty;
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Implementation of PayPal webhook signature verification using PayPal's REST API.
///     Calls POST /v1/notifications/verify-webhook-signature to validate incoming webhooks.
/// </summary>
public class PayPalSignatureVerificationService : IPayPalSignatureVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly PayPalSettings _settings;
    private readonly ILogger<PayPalSignatureVerificationService> _logger;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PayPalSignatureVerificationService(
        HttpClient httpClient,
        IOptions<PayPalSettings> settings,
        ILogger<PayPalSignatureVerificationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PayPalVerificationResult> VerifySignatureAsync(
        string webhookId,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string? certUrl,
        string? authAlgo,
        string webhookEventBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(webhookId))
        {
            _logger.LogWarning("PayPal webhook ID not configured, skipping signature verification");
            return PayPalVerificationResult.Failed("Webhook ID not configured");
        }

        try
        {
            // Get access token
            var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(accessToken))
            {
                return PayPalVerificationResult.Failed("Failed to obtain PayPal access token");
            }

            // Build verification request
            var verificationRequest = new PayPalVerifyWebhookRequest
            {
                AuthAlgo = authAlgo ?? "SHA256withRSA",
                CertUrl = certUrl ?? string.Empty,
                TransmissionId = transmissionId,
                TransmissionSig = transmissionSig,
                TransmissionTime = transmissionTime,
                WebhookId = webhookId,
                WebhookEvent = JsonDocument.Parse(webhookEventBody)
            };

            var requestJson = JsonSerializer.Serialize(verificationRequest, PayPalJsonContext.Default.PayPalVerifyWebhookRequest);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/notifications/verify-webhook-signature")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            _logger.LogDebug("Verifying PayPal webhook signature. TransmissionId={TransmissionId}", transmissionId);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PayPal signature verification API returned {StatusCode}: {Response}",
                    response.StatusCode, responseContent);
                return PayPalVerificationResult.Failed($"API returned {response.StatusCode}");
            }

            // Parse response
            var verificationResponse = JsonSerializer.Deserialize(responseContent, PayPalJsonContext.Default.PayPalVerifyWebhookResponse);

            if (verificationResponse?.VerificationStatus == "SUCCESS")
            {
                _logger.LogInformation(
                    "PayPal webhook signature verified successfully. TransmissionId={TransmissionId}",
                    transmissionId);
                return PayPalVerificationResult.Success();
            }

            _logger.LogWarning(
                "PayPal webhook signature verification failed. TransmissionId={TransmissionId}, Status={Status}",
                transmissionId, verificationResponse?.VerificationStatus);
            return PayPalVerificationResult.Failed($"Verification status: {verificationResponse?.VerificationStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayPal webhook signature. TransmissionId={TransmissionId}", transmissionId);
            return PayPalVerificationResult.Failed(ex.Message);
        }
    }

    /// <summary>
    ///     Gets an OAuth2 access token from PayPal.
    ///     Caches the token until it expires.
    /// </summary>
    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Check cached token
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiresAt)
        {
            return _cachedAccessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiresAt)
            {
                return _cachedAccessToken;
            }

            // Get new token
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials"
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to obtain PayPal access token: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize(responseContent, PayPalJsonContext.Default.PayPalTokenResponse);

            if (tokenResponse?.AccessToken != null)
            {
                _cachedAccessToken = tokenResponse.AccessToken;
                // Expire token 5 minutes early to avoid edge cases
                _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);
                return _cachedAccessToken;
            }

            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}

/// <summary>Request payload for PayPal verify-webhook-signature API</summary>
internal class PayPalVerifyWebhookRequest
{
    [JsonPropertyName("auth_algo")]
    public string AuthAlgo { get; set; } = string.Empty;

    [JsonPropertyName("cert_url")]
    public string CertUrl { get; set; } = string.Empty;

    [JsonPropertyName("transmission_id")]
    public string TransmissionId { get; set; } = string.Empty;

    [JsonPropertyName("transmission_sig")]
    public string TransmissionSig { get; set; } = string.Empty;

    [JsonPropertyName("transmission_time")]
    public string TransmissionTime { get; set; } = string.Empty;

    [JsonPropertyName("webhook_id")]
    public string WebhookId { get; set; } = string.Empty;

    [JsonPropertyName("webhook_event")]
    public JsonDocument? WebhookEvent { get; set; }
}

/// <summary>Response from PayPal verify-webhook-signature API</summary>
internal class PayPalVerifyWebhookResponse
{
    [JsonPropertyName("verification_status")]
    public string VerificationStatus { get; set; } = string.Empty;
}

/// <summary>Response from PayPal OAuth2 token endpoint</summary>
internal class PayPalTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>JSON serialization context for PayPal API types</summary>
[JsonSerializable(typeof(PayPalVerifyWebhookRequest))]
[JsonSerializable(typeof(PayPalVerifyWebhookResponse))]
[JsonSerializable(typeof(PayPalTokenResponse))]
internal partial class PayPalJsonContext : JsonSerializerContext
{
}

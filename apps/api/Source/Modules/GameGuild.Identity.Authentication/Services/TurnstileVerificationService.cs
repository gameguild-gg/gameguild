using System.Text.Json;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Cloudflare Turnstile token verification via the siteverify HTTP endpoint
///     (issue #1082). Behavior:
///     <list type="bullet">
///         <item>Secret unset → disabled: success with <c>disabled</c> (dev/CI fail-open), warned once per process.</item>
///         <item>Missing/oversized token → fail-closed with <c>missing-token</c>.</item>
///         <item>Cloudflare responds success:false → fail-closed with <c>verify-failed</c>.</item>
///         <item>Network/parse error → fail-open with <c>verify-unavailable</c> (an outage must not lock everyone out; an explicit Cloudflare rejection does).</item>
///     </list>
/// </summary>
public sealed class TurnstileVerificationService(
    IHttpClientFactory httpClientFactory,
    AuthenticationSecurityOptions options,
    ILogger<TurnstileVerificationService> logger) : ITurnstileVerificationService
{
    /// <summary>The named HTTP client used for siteverify calls (registered with a timeout).</summary>
    public const string HttpClientName = "GameGuild.Turnstile";

    private const int MaxTokenLength = 2048;

    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    private bool disabledWarningLogged;

    public async Task<TurnstileVerificationResult> VerifyAsync(
        string? token,
        string? remoteIpAddress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var secret = options.TurnstileSecretKey;
        if (string.IsNullOrWhiteSpace(secret))
        {
            if (!disabledWarningLogged)
            {
                disabledWarningLogged = true;
                logger.LogWarning("Turnstile secret not set; sign-in CAPTCHA verification disabled");
            }

            return TurnstileVerificationResult.Disabled;
        }

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return TurnstileVerificationResult.MissingToken;
        }

        var verifyUrl = string.IsNullOrWhiteSpace(options.TurnstileSiteVerifyUrl)
            ? "https://challenges.cloudflare.com/turnstile/v0/siteverify"
            : options.TurnstileSiteVerifyUrl;

        TurnstileSiteVerifyResponse? result;
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(BuildSiteVerifyPayload(secret, token, remoteIpAddress));
            using var response = await client.PostAsync(verifyUrl, content, cancellationToken).ConfigureAwait(false);
            // A non-success HTTP status is still a parsed Cloudflare response whenever the
            // body is readable; only transport/parsing failures fail open.
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            result = TryDeserialize(payload);
            if (result is null)
            {
                logger.LogError("Turnstile siteverify returned an unreadable response (HTTP {Status})", (int)response.StatusCode);
                return TurnstileVerificationResult.VerifyUnavailable;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Turnstile siteverify request failed; allowing request");
            return TurnstileVerificationResult.VerifyUnavailable;
        }

        return result.Success is true
            ? new TurnstileVerificationResult(true)
            : TurnstileVerificationResult.VerifyFailed;
    }

    private static Dictionary<string, string> BuildSiteVerifyPayload(string secret, string token, string? remoteIpAddress)
    {
        var payload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["secret"] = secret,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            payload["remoteip"] = remoteIpAddress;
        }

        return payload;
    }

    private static TurnstileSiteVerifyResponse? TryDeserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<TurnstileSiteVerifyResponse>(payload, ResponseJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class TurnstileSiteVerifyResponse
    {
        public bool? Success { get; set; }
    }
}

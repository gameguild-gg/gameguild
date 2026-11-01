using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Cloudflare Turnstile verifier (issue #1150). Enforcement is opt-in per
///     deployment: verification is required exactly when a secret is configured
///     (Turnstile:SecretKey, or the TURNSTILE_SECRET_KEY environment variable used by
///     the web app), so dev/CI environments without a secret are unaffected.
/// </summary>
public sealed class TurnstileVerificationService(
    IConfiguration configuration,
    ILogger<TurnstileVerificationService> logger) : ITurnstileVerificationService
{
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private const int MaxTokenLength = 2048;

    // One shared client for the process; verification calls are rare (sign-up only) and
    // the static-siteverify endpoint needs no per-request disposal.
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly JsonSerializerOptions ResponseJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string? _secretKey = ResolveSecret(configuration);

    private bool _disabledWarningLogged;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_secretKey);

    public async Task<bool> VerifyAsync(string? turnstileToken, string? remoteIpAddress = null, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            if (!_disabledWarningLogged)
            {
                _disabledWarningLogged = true;
                logger.LogWarning("Turnstile secret not configured; server-side anti-bot verification is disabled");
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(turnstileToken) || turnstileToken.Length > MaxTokenLength)
        {
            logger.LogWarning("Sign-up blocked: Turnstile token missing or malformed");
            return false;
        }

        try
        {
            var payload = new { secret = _secretKey, response = turnstileToken, remoteip = remoteIpAddress };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync(SiteVerifyUrl, content, cancellationToken).ConfigureAwait(false);

            var siteVerify = await JsonSerializer
                .DeserializeAsync<SiteVerifyResponse>(response.Content.ReadAsStream(cancellationToken), ResponseJsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (siteVerify?.Success == true)
            {
                return true;
            }

            logger.LogWarning("Sign-up blocked: Cloudflare rejected the Turnstile token");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fail-open on verifier unavailability: a Cloudflare/network outage must not
            // lock every sign-up out — an explicit Cloudflare rejection (above) does.
            logger.LogError(ex, "Turnstile siteverify request failed; allowing the request");
            return true;
        }
    }

    private static string? ResolveSecret(IConfiguration configuration)
    {
        var secret = configuration["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = Environment.GetEnvironmentVariable("TURNSTILE_SECRET_KEY");
        }

        return string.IsNullOrWhiteSpace(secret) ? null : secret.Trim();
    }

    private sealed record SiteVerifyResponse
    {
        public bool? Success { get; init; }
    }
}

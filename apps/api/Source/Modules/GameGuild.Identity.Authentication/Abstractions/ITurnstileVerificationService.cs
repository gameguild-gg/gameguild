namespace GameGuild.Identity.Authentication;

/// <summary>Outcome of a server-side Cloudflare Turnstile token verification.</summary>
public sealed record TurnstileVerificationResult(bool Success, string? Reason = null)
{
    /// <summary>Verification is not configured (no secret key); the gate is disabled.</summary>
    public static TurnstileVerificationResult Disabled { get; } = new(true, "disabled");

    /// <summary>The token was missing or malformed.</summary>
    public static TurnstileVerificationResult MissingToken { get; } = new(false, "missing-token");

    /// <summary>Cloudflare explicitly rejected the token.</summary>
    public static TurnstileVerificationResult VerifyFailed { get; } = new(false, "verify-failed");

    /// <summary>Cloudflare could not be reached; the request is allowed (fail-open).</summary>
    public static TurnstileVerificationResult VerifyUnavailable { get; } = new(true, "verify-unavailable");
}

/// <summary>
///     Server-side verification of Cloudflare Turnstile tokens (issue #1082). Semantics
///     mirror the web tier's verifier: an unset secret disables the gate; a missing or
///     oversized token fails closed; an explicit Cloudflare rejection fails closed; a
///     network or parsing error fails OPEN because a Turnstile outage must not lock
///     every user out of their account.
/// </summary>
public interface ITurnstileVerificationService
{
    /// <summary>
    ///     Verifies a Turnstile token against the configured siteverify endpoint.
    /// </summary>
    /// <param name="token">The client-supplied token (may be null when absent).</param>
    /// <param name="remoteIpAddress">The client IP passed to Cloudflare for replay checks (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TurnstileVerificationResult> VerifyAsync(
        string? token,
        string? remoteIpAddress = null,
        CancellationToken cancellationToken = default);
}

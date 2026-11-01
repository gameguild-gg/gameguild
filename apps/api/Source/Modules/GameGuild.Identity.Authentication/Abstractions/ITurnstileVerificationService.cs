namespace GameGuild.Identity.Authentication;

/// <summary>
///     Server-side verification of Cloudflare Turnstile anti-bot tokens (issue #1150).
///     The Next.js layer enforced Turnstile only client-side of the web app, so direct
///     API callers bypassed it entirely; the API must enforce it itself.
/// </summary>
public interface ITurnstileVerificationService
{
    /// <summary>
    ///     True when a Turnstile secret is configured and verification is therefore
    ///     enforced for endpoints protected by this service.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     Verifies a Turnstile token against Cloudflare's siteverify endpoint.
    ///     Semantics (mirroring the web app's helper):
    ///     <list type="bullet">
    ///         <item>Secret unset (no Turnstile:SecretKey / TURNSTILE_SECRET_KEY) — disabled: returns true (fail-open, warned once).</item>
    ///         <item>Missing or oversized token — returns false (fail-closed).</item>
    ///         <item>Cloudflare rejects the token — returns false (fail-closed).</item>
    ///         <item>Network/parse error reaching Cloudflare — returns true (fail-open: an outage must not lock every sign-up out; an explicit rejection does).</item>
    ///     </list>
    /// </summary>
    /// <param name="turnstileToken">Token produced by the Turnstile widget, sent by the client.</param>
    /// <param name="remoteIpAddress">Optional client IP passed to Cloudflare for token binding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> VerifyAsync(string? turnstileToken, string? remoteIpAddress = null, CancellationToken cancellationToken = default);
}

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Password sign-in admission denied the request because a Cloudflare Turnstile
///     token is required after repeated failed attempts from the source IP (issue
///     #1082) and the presented token was missing or rejected. The message carries the
///     stable marker <see cref="DetailMarker"/> so clients can render the CAPTCHA
///     challenge; the condition depends only on the source IP's failure count and
///     never discloses account state.
/// </summary>
public sealed class PasswordSignInCaptchaRequiredException : UnauthorizedAccessException
{
    /// <summary>
    ///     Stable marker embedded in the denial detail; clients match it to reveal the
    ///     CAPTCHA challenge on the sign-in form.
    /// </summary>
    public const string DetailMarker = "captcha_required";

    public PasswordSignInCaptchaRequiredException() : base(
        $"Bot verification is required before signing in. Complete the CAPTCHA challenge and try again. ({DetailMarker})")
    {
    }
}

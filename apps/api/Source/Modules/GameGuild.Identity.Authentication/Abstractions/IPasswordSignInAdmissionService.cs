using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Admits a password attempt for its canonical account identifier and request source.
/// The caller retains the returned lease through authentication and recording.
/// A denied attempt performs timing compensation and throws the generic authentication failure.
/// </summary>
public interface IPasswordSignInAdmissionService
{
    /// <summary>
    ///     Admits the attempt. When the source IP has enough recent failed attempts
    ///     (issue #1082), a Turnstile token must be supplied and verify; otherwise a
    ///     <see cref="PasswordSignInCaptchaRequiredException"/> is thrown.
    /// </summary>
    /// <param name="identifier">The normalized account identifier (email).</param>
    /// <param name="context">The request context used for the client IP.</param>
    /// <param name="timingOrigin">The request's timing origin for denial compensation.</param>
    /// <param name="turnstileToken">Optional Turnstile token presented by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IAsyncDisposable> AdmitAsync(string identifier, HttpContext? context,
        AuthenticationTimingOrigin timingOrigin, CancellationToken cancellationToken,
        string? turnstileToken = null);
}

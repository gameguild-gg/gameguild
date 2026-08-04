namespace GameGuild.Identity.Authentication;

/// <summary>
///     Cryptographically verifies a Google ID token (signature, <c>iss</c>,
///     <c>aud</c>, <c>exp</c>) and returns the validated claims as a
///     <see cref="VerifiedGoogleUser" />. Throws
///     <see cref="UnauthorizedAccessException" /> for any malformed, forged,
///     expired, or wrong-audience token — callers should surface that as 401.
/// </summary>
public interface IGoogleIdTokenVerifier
{
    /// <param name="idToken">The raw Google ID token (JWT) from the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UnauthorizedAccessException">
    ///     The token is null/empty, malformed, signature-invalid, expired,
    ///     or audience-mismatched.
    /// </exception>
    Task<VerifiedGoogleUser> VerifyAsync(string idToken, CancellationToken ct);
}

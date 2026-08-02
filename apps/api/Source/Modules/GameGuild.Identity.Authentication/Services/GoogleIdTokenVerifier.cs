using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Verifies Google ID tokens cryptographically via
///     <see cref="GoogleJsonWebSignature" />. This SUPERSEDES the legacy
///     <c>OAuthService.ValidateGoogleIdTokenInternalAsync</c> HTTP-lookup
///     path, which only hit Google's <c>tokeninfo</c> endpoint and could not
///     reject a forged or expired token. The library validates signature
///     (Google JWKS), <c>iss</c>, <c>aud</c> (against the configured ClientId)
///     and <c>exp</c>; we just map the validated payload.
/// </summary>
public sealed class GoogleIdTokenVerifier : IGoogleIdTokenVerifier
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleIdTokenVerifier> _logger;

    public GoogleIdTokenVerifier(IConfiguration configuration, ILogger<GoogleIdTokenVerifier> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<VerifiedGoogleUser> VerifyAsync(string idToken, CancellationToken ct)
    {
        // Fail closed: empty clientId ⇒ ValidateAsync's audience check would
        // reject everything anyway, but rejecting here avoids a network round
        // trip and makes the misconfiguration explicit in logs.
        var clientId = _configuration["OAuth:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Google ID token verifier rejecting token — OAuth:Google:ClientId not configured");
            throw new UnauthorizedAccessException("Google OAuth ClientId is not configured");
        }

        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new UnauthorizedAccessException("Google ID token is empty");
        }

        // Google.Apis.Auth.ValidateAsync has no CancellationToken overload — see XML doc.
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } }
            ).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            // Wrap every library failure (InvalidJwtException, JWKS fetch errors,
            // aud/iss/exp mismatch, signature mismatch) into a single 401-equivalent
            // so callers cannot distinguish malformed from forged — both are 401.
            _logger.LogWarning(ex, "Google ID token validation rejected a token");
            throw new UnauthorizedAccessException("Google ID token is invalid", ex);
        }

        // The Payload is null only if validation was bypassed — defensive guard.
        if (payload is null)
        {
            throw new UnauthorizedAccessException("Google ID token validation returned no payload");
        }

        return new VerifiedGoogleUser
        {
            Sub = payload.Subject,
            Email = payload.Email ?? string.Empty,
            EmailVerified = payload.EmailVerified,
            Name = payload.Name,
            Picture = payload.Picture
        };
    }
}

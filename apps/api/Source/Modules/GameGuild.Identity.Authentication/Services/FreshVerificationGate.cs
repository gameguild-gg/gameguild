using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Default <see cref="IFreshVerificationGate"/>: trusted-device mutations require a recent
///     authentication (<c>auth_time</c> claim within the session-options window) and, when the
///     acting user has MFA enabled, a token that carries a verified MFA proof (issue #1144).
///     Fail closed: an actor context without a usable auth_time never passes the gate.
/// </summary>
public sealed class FreshVerificationGate(
    IActorContextAccessor actorContextAccessor,
    IMfaService mfaService,
    SessionOptions? sessionOptions = null,
    ILogger<FreshVerificationGate>? logger = null) : IFreshVerificationGate
{
    private readonly SessionOptions _sessionOptions = sessionOptions ?? new SessionOptions();

    public async Task<FreshVerificationResult> VerifyRecentAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionOptions.RequireFreshAuthenticationForDeviceTrust)
        {
            return FreshVerificationResult.Verified;
        }

        var actor = actorContextAccessor.ActorContext;
        if (actor?.IsAuthenticated != true || actor.SubjectIdAsGuid is not { } actorId)
        {
            logger?.LogWarning("Trusted-device fresh verification denied: no authenticated actor context");
            return FreshVerificationResult.Failed("Re-authentication is required to manage trusted devices.");
        }

        // The access token's auth_time is the moment the user last proved their credential;
        // a stale value means the session predates the configured freshness window.
        if (actor.TypedAttributes.AuthenticatedAt is not { } authenticatedAt ||
            authenticatedAt < SystemClock.UtcNow.Subtract(
                TimeSpan.FromMinutes(Math.Max(1, _sessionOptions.DeviceTrustFreshAuthenticationWindowMinutes))))
        {
            logger?.LogInformation(
                "Trusted-device fresh verification denied for user {UserId}: authentication is not recent",
                LogRedaction.RedactId(actorId, "uid"));
            return FreshVerificationResult.Failed("Re-authentication is required to manage trusted devices.");
        }

        // Users with MFA configured must present an MFA-verified token for the mutation.
        bool mfaEnabled;
        try
        {
            mfaEnabled = await mfaService.IsMfaEnabledAsync(actorId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger?.LogError(exception, "Trusted-device fresh verification could not resolve the MFA state for user {UserId}", LogRedaction.RedactId(actorId, "uid"));
            return FreshVerificationResult.Failed("Re-authentication is required to manage trusted devices.");
        }

        if (mfaEnabled && !actor.IsMfaVerified)
        {
            logger?.LogInformation(
                "Trusted-device fresh verification denied for user {UserId}: MFA-enabled account without a verified MFA token",
                LogRedaction.RedactId(actorId, "uid"));
            return FreshVerificationResult.Failed("Re-authenticate with MFA to manage trusted devices.");
        }

        return FreshVerificationResult.Verified;
    }
}

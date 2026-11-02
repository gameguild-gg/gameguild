using GameGuild.Configuration.ApplicationLayer;
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Names the authorization policy that requires the acting user's current session to run
///     on a trusted device (<c>Session:RequireTrustedDeviceForSensitiveOps</c>, issue #278).
/// </summary>
public static class TrustedDevicePolicy
{
    public const string Name = "require-trusted-device";
}

/// <summary>
///     Authorization requirement marking an operation as sensitive enough that, when
///     <see cref="SessionOptions.RequireTrustedDeviceForSensitiveOps"/> is enabled, it may
///     only be performed from a session whose device fingerprint is currently trusted.
/// </summary>
public sealed class TrustedDeviceRequirement : IAuthorizationRequirement
{
}

/// <summary>
///     Enforces <see cref="TrustedDeviceRequirement"/> against the persisted session and
///     trusted-device registries.
///     <para>
///         The requirement only constrains interactive user sessions (requests carrying a
///         <c>session_id</c> claim). Requests authenticated through other schemes — API keys,
///         service accounts, guests — are intentionally passed through: they have no user
///         session whose device could be trusted, and their own policies govern them.
///     </para>
///     <para>
///         When <see cref="SessionOptions.RequireTrustedDeviceForSensitiveOps"/> is disabled
///         (the default), the requirement succeeds for everyone, so applying the policy never
///         changes behavior until the option is turned on.
///     </para>
/// </summary>
public sealed class TrustedDeviceAuthorizationHandler(
    ISessionManagementService sessionService,
    SessionOptions? sessionOptions = null) : AuthorizationHandler<TrustedDeviceRequirement>
{
    private readonly SessionOptions _sessionOptions = sessionOptions ?? new SessionOptions();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TrustedDeviceRequirement requirement)
    {
        if (!_sessionOptions.RequireTrustedDeviceForSensitiveOps)
        {
            context.Succeed(requirement);
            return;
        }

        var sessionIdClaim = context.User.FindFirst(JwtClaimTypes.SessionId)?.Value;
        var userIdClaim = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sessionIdClaim) || string.IsNullOrWhiteSpace(userIdClaim) ||
            !Guid.TryParse(sessionIdClaim, out var sessionId) || sessionId == Guid.Empty ||
            !Guid.TryParse(userIdClaim, out var userId) || userId == Guid.Empty)
        {
            // Not an interactive user session: other schemes' own policies govern these.
            context.Succeed(requirement);
            return;
        }

        try
        {
            var session = await sessionService.GetSessionAsync(sessionId).ConfigureAwait(false);
            if (session is not { IsActive: true } ||
                session.TerminatedAt.HasValue ||
                session.UserId != userId ||
                string.IsNullOrWhiteSpace(session.DeviceFingerprint))
            {
                context.Fail(new AuthorizationFailureReason(
                    this,
                    "Sensitive operations require a device trust that could not be verified."));
                return;
            }

            if (await sessionService.IsDeviceTrustedAsync(userId, session.DeviceFingerprint).ConfigureAwait(false))
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail(new AuthorizationFailureReason(
                this,
                "Sensitive operations require a trusted device; trust this device first."));
        }
        catch (Exception exception)
        {
            // Fail closed: a registry outage must not silently waive the device-trust gate.
            context.Fail(new AuthorizationFailureReason(
                this,
                $"Device trust verification failed: {exception.GetType().Name}"));
        }
    }
}

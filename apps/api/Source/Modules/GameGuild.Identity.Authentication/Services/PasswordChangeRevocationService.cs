using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Credential-change revocation battery for password change/reset (issue #278). Mirrors
///     <c>RevokeAllUserTokensHandler</c> and <c>RiskEventSessionRevocationHandler</c>: refresh
///     tokens are revoked eagerly (a stolen refresh token must not outlive the password that
///     guarded it), sessions terminate through <see cref="ISessionManagementService"/> so the
///     per-session audit events and session analytics metrics flow, and a versioned user-token
///     revocation is recorded to cover legacy access tokens without version/session claims.
///     The token version itself was already bumped by <c>User.SetPasswordHash</c> inside the
///     password update that precedes this call; this reads the persisted version back.
/// </summary>
public sealed class PasswordChangeRevocationService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ISessionManagementService sessionManagementService,
    IVersionedUserTokenRevocationService tokenRevocationService,
    IRefreshTokenLifecycleRecorder lifecycleRecorder,
    ILogger<PasswordChangeRevocationService> logger) : IPasswordChangeRevocationService
{
    public async Task<int> RevokeAllSessionsAndTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        cancellationToken.ThrowIfCancellationRequested();
        RefreshTokenLifecycleMetrics.RecordAttempt(RefreshTokenLifecycleOperation.AllRevoked);
        await refreshTokenRepository.RevokeAllForUserAsync(userId, revokedByIp: null, cancellationToken)
            .ConfigureAwait(false);
        var terminatedSessions = await sessionManagementService.TerminateAllUserSessionsAsync(
                userId, SessionTerminationReason.PasswordChanged, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await lifecycleRecorder.RecordMutationAsync(
                new RefreshTokenLifecycleEvent(RefreshTokenLifecycleOperation.AllRevoked, userId, TenantId: null),
                cancellationToken)
            .ConfigureAwait(false);

        // The password update already bumped the persisted token version (User.SetPasswordHash);
        // read it back so the versioned cutoff also covers tokens issued before this version.
        // A missing version (user vanished concurrently) resolves to 0, which revokes every
        // previously issued token — fail closed.
        var tokenVersion = await userRepository.GetTokenVersionAsync(userId, cancellationToken).ConfigureAwait(false) ?? 0;
        // A store failure must not report the revocation as complete: it propagates.
        await tokenRevocationService.RevokeAllUserTokensAsync(userId, tokenVersion, reason, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Password change revocation applied for user {UserId}: {TerminatedSessions} session(s) terminated, all refresh tokens revoked, versioned token cutoff at version {TokenVersion}",
            userId, terminatedSessions, tokenVersion);

        return terminatedSessions;
    }
}

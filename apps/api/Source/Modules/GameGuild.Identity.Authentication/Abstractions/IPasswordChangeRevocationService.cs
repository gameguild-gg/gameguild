namespace GameGuild.Identity.Authentication;

/// <summary>
///     Full credential-invalidating revocation triggered by a password change or reset
///     (issue #278): every refresh token is revoked, every active session is terminated
///     with an audited <see cref="SessionTerminationReason.PasswordChanged"/> reason, and a
///     versioned user-token revocation is recorded so legacy access tokens without
///     version/session claims stop validating immediately.
/// </summary>
public interface IPasswordChangeRevocationService
{
    /// <summary>
    ///     Revokes every refresh token for the user, terminates every active session and
    ///     records the versioned user cutoff. Returns the number of terminated sessions.
    ///     A revocation-store failure propagates: the operation must not be reported as
    ///     complete when the cutoff could not be recorded.
    /// </summary>
    /// <param name="userId">The user whose credentials just changed.</param>
    /// <param name="reason">Human-readable revocation reason recorded with the token cutoff.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> RevokeAllSessionsAndTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Password re-verification for MFA state mutations (issue #1153). backup-codes
///     regeneration and push-device registration mutate MFA state with only a bearer
///     token; a stolen token must not be able to rotate recovery codes (locking the real
///     user out of emergency access) or hijack push delivery. The account password is
///     required first — the same hardening the MFA disable flow applies (issue #244).
/// </summary>
public interface IMfaReverificationService
{
    /// <summary>
    ///     Verifies the account password for the given user. Passwordless accounts (no
    ///     stored hash) always fail: they hold no password to re-verify with.
    /// </summary>
    /// <param name="userId">User whose password is being re-verified.</param>
    /// <param name="password">Password supplied by the caller.</param>
    /// <param name="operation">Operation label used in refusal logs/metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the password matches the stored hash.</returns>
    Task<bool> VerifyPasswordAsync(Guid userId, string password, string operation, CancellationToken cancellationToken = default);
}

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records the <see cref="AccountLockoutDetectedV1"/> durable event when the account
///     lockout policy engages (issue #1083). Delivery is preference-gated host-side, so
///     this publisher only durably records the fact of the lockout; it never blocks the
///     (already denied) sign-in request.
/// </summary>
public interface IAccountLockoutAlertPublisher
{
    /// <summary>
    ///     Records the account-lockout event for the locked account. Defense-in-depth:
    ///     failures are logged and swallowed.
    /// </summary>
    /// <param name="userId">The owner of the locked account.</param>
    /// <param name="lockoutEndsAtUtc">UTC timestamp at which sign-in becomes possible again.</param>
    /// <param name="failedAttemptThreshold">The configured failure threshold that engaged the lockout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(
        Guid userId,
        DateTime lockoutEndsAtUtc,
        int failedAttemptThreshold,
        CancellationToken cancellationToken = default);
}

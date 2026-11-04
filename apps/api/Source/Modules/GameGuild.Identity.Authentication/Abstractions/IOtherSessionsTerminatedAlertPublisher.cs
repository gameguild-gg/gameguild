namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records <see cref="OtherSessionsTerminatedV1"/> durable events for the host-side
///     notification bridge (issue #1091). Published when a password change or reset
///     terminated the account's other sessions, so the owner receives the confirmation
///     email instead of silently losing every signed-in device.
/// </summary>
public interface IOtherSessionsTerminatedAlertPublisher
{
    /// <summary>
    ///     Records the termination event. Failures are swallowed by implementations:
    ///     the notification is defense-in-depth and must never fail the password operation.
    /// </summary>
    /// <param name="userId">The user whose sessions were terminated.</param>
    /// <param name="tenantId">The tenant active for the operation, when one is known.</param>
    /// <param name="reason">Coarse reason code recorded with the event (for example <c>PasswordChanged</c>).</param>
    /// <param name="terminatedSessionCount">Number of sessions that were terminated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(
        Guid userId,
        Guid? tenantId,
        string reason,
        int terminatedSessionCount,
        CancellationToken cancellationToken = default);
}

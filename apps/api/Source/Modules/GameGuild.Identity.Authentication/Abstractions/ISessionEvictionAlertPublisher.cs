namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records the <see cref="SessionEvictionDetectedV1"/> durable event when the
///     concurrent session limit evicts an older session (issue #1086). Delivery is
///     preference-gated host-side; this publisher only durably records the fact of the
///     eviction and never blocks session creation.
/// </summary>
public interface ISessionEvictionAlertPublisher
{
    /// <summary>
    ///     Records the session-eviction event for the terminated session. Defense-in-depth:
    ///     failures are logged and swallowed.
    /// </summary>
    /// <param name="userId">The session's owner.</param>
    /// <param name="sessionId">The terminated session.</param>
    /// <param name="deviceSummary">Coarse parsed device summary of the terminated session.</param>
    /// <param name="maxConcurrentSessions">The configured concurrent session limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(
        Guid userId,
        Guid sessionId,
        string? deviceSummary,
        int maxConcurrentSessions,
        CancellationToken cancellationToken = default);
}

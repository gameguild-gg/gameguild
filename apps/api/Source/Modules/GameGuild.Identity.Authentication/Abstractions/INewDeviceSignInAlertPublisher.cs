namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records the <see cref="NewDeviceSignInDetectedV1"/> durable event when a session
///     is created with a first-seen device fingerprint (issue #1085). Delivery is
///     preference-gated host-side; this publisher only durably records the fact of the
///     new device and never blocks authentication.
/// </summary>
public interface INewDeviceSignInAlertPublisher
{
    /// <summary>
    ///     Records the new-device event for the created session. Defense-in-depth:
    ///     failures are logged and swallowed.
    /// </summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="sessionId">The newly created session.</param>
    /// <param name="deviceSummary">Coarse parsed device summary (never the raw agent).</param>
    /// <param name="locationSummary">Coarse session location (city/country), when resolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(
        Guid userId,
        Guid sessionId,
        string? deviceSummary,
        string? locationSummary,
        CancellationToken cancellationToken = default);
}

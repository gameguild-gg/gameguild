
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Interface for Security Information and Event Management (SIEM) integration.
///     Sends security events to external SIEM systems for centralized monitoring and alerting.
/// </summary>
public interface ISiemIntegrationService
{
    /// <summary>
    ///     Sends a security event to the SIEM system.
    /// </summary>
    Task SendSecurityEventAsync(SiemEvent siemEvent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends an authentication anomaly event to the SIEM system.
    /// </summary>
    Task SendAnomalyEventAsync(AuthenticationAttempt attempt, AuthenticationAttemptAnalysis analysis, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a suspicious activity event to the SIEM system.
    /// </summary>
    Task SendSuspiciousActivityEventAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a brute force attack detection event to the SIEM system.
    /// </summary>
    Task SendBruteForceEventAsync(string identifier, int attemptCount, TimeSpan timeWindow, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends an impossible travel detection event to the SIEM system.
    /// </summary>
    Task SendImpossibleTravelEventAsync(Guid userId, LocationInfo fromLocation, LocationInfo toLocation, TimeSpan timeBetween, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if SIEM integration is enabled.
    /// </summary>
    bool IsEnabled();
}

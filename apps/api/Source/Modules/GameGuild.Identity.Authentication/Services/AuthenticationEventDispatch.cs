using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Dispatch helpers for the module's in-process authentication notifications
///     (docs/auth/event-flows.md §2.2–2.3). Publishing is best-effort: these notifications
///     are log/telemetry side channels, so a dispatch failure is logged and never fails
///     the authentication flow that produced it.
/// </summary>
internal static class AuthenticationEventDispatch
{
    /// <summary>
    ///     Publishes an in-process authentication notification, swallowing non-cancellation
    ///     dispatch failures after logging them (when a logger is supplied).
    /// </summary>
    public static async Task PublishAsync<TNotification>(
        IPublisher? publisher,
        TNotification notification,
        ILogger? logger,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        if (publisher is null)
        {
            return;
        }

        try
        {
            await publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger?.LogError(exception, "Could not dispatch {EventType} in-process event", typeof(TNotification).Name);
        }
    }

    /// <summary>
    ///     Forwards the confirmed containment-class signals of a sign-in analysis (impossible
    ///     travel, brute force) to the durable suspicious-login alert publisher, whose host-side
    ///     consumers revoke sessions and tokens. The publisher gates on the configured severity
    ///     and swallows its own failures; a missing analysis or publisher is a no-op.
    /// </summary>
    public static async Task AlertConfirmedSignInRisksAsync(
        ISuspiciousLoginAlertPublisher? alerts,
        Guid userId,
        Guid? tenantId,
        AuthenticationAnomalyResult? analysis,
        CancellationToken cancellationToken)
    {
        if (alerts is null || analysis is null)
        {
            return;
        }

        if (analysis.DetectedAnomalies.Contains(SecurityAlertKinds.ImpossibleTravel, StringComparer.Ordinal))
        {
            await alerts.RecordAsync(
                userId, tenantId, SecurityAlertKinds.ImpossibleTravel, analysis.RiskLevel, analysis.RiskScore, cancellationToken).ConfigureAwait(false);
        }

        if (analysis.DetectedAnomalies.Contains(SecurityAlertKinds.BruteForceDetected, StringComparer.Ordinal))
        {
            await alerts.RecordAsync(
                userId, tenantId, SecurityAlertKinds.BruteForceDetected, analysis.RiskLevel, analysis.RiskScore, cancellationToken).ConfigureAwait(false);
        }
    }
}

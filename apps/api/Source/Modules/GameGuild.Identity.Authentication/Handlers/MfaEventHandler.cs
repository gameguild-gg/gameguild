using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for logging MFA-related events
/// </summary>
public class MfaEventHandler(ILogger<MfaEventHandler> logger) : INotificationHandler<MfaEnabledEvent>,
    INotificationHandler<MfaDisabledEvent>,
    INotificationHandler<MfaVerificationSucceededEvent>,
    INotificationHandler<MfaVerificationFailedEvent>
{
    public async Task Handle(MfaDisabledEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("MFA disabled for user {UserId} (method: {Method}) at {DisabledAt}", notification.UserId, notification.Method, notification.DisabledAt);

        // Here you could also:
        // - Send warning email about MFA being disabled
        // - Require additional verification for sensitive actions
        // - Log security audit trail

        await Task.CompletedTask;
    }

    public async Task Handle(MfaEnabledEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("MFA enabled for user {UserId} using method {Method} at {EnabledAt}", notification.UserId, notification.Method, notification.EnabledAt);

        // Here you could also:
        // - Send confirmation email about MFA being enabled
        // - Update user security settings
        // - Log security audit trail

        await Task.CompletedTask;
    }

    public async Task Handle(MfaVerificationFailedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning("MFA verification failed for user {UserId} using method {Method}. Reason: {Reason} at {AttemptedAt}", notification.UserId, notification.Method, notification.Reason, notification.AttemptedAt);

        // Here you could also:
        // - Increment failed attempt counter
        // - Lock account after too many failures
        // - Send security alert email
        // - Log to security monitoring system

        await Task.CompletedTask;
    }

    public async Task Handle(MfaVerificationSucceededEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("MFA verification succeeded for user {UserId} using method {Method} at {VerifiedAt}", notification.UserId, notification.Method, notification.VerifiedAt);

        // Here you could also:
        // - Update last successful MFA timestamp
        // - Clear failed attempt counter
        // - Log successful verification for audit

        await Task.CompletedTask;
    }
}

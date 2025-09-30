using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for logging MFA-related events </summary>
public class MfaEventHandler(ILogger<MfaEventHandler> logger) : INotificationHandler<MfaEnabledEvent>,
    INotificationHandler<MfaDisabledEvent>,
    INotificationHandler<MfaVerificationSucceededEvent>,
    INotificationHandler<MfaVerificationFailedEvent>
{
    public async Task Handle(MfaEnabledEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("MFA enabled for user {UserId} using method {Method} at {EnabledAt}", notification.UserId, notification.Method, notification.EnabledAt);

        await Task.CompletedTask;
    }

    public async Task Handle(MfaDisabledEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("MFA disabled for user {UserId} (method: {Method}) at {DisabledAt}", notification.UserId, notification.Method, notification.DisabledAt);

        await Task.CompletedTask;
    }

    public async Task Handle(MfaVerificationSucceededEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("MFA verification succeeded for user {UserId} using method {Method} at {VerifiedAt}", notification.UserId, notification.Method, notification.VerifiedAt);

        await Task.CompletedTask;
    }

    public async Task Handle(MfaVerificationFailedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning("MFA verification failed for user {UserId} using method {Method}. Reason: {Reason} at {AttemptedAt}", notification.UserId, notification.Method, notification.Reason, notification.AttemptedAt);

        await Task.CompletedTask;
    }
}

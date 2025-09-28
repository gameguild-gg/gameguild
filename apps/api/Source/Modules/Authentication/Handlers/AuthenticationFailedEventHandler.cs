using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for logging authentication failure events </summary>
public class AuthenticationFailedEventHandler(ILogger<AuthenticationFailedEventHandler> logger) : INotificationHandler<AuthenticationFailedEvent>
{
    public async Task Handle(AuthenticationFailedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Authentication failed for email {Email}. Reason: {Reason}. IP: {IpAddress}, User Agent: {UserAgent}, Time: {AttemptedAt}",
            notification.Email,
            notification.Reason,
            notification.IpAddress ?? "Unknown",
            notification.UserAgent ?? "Unknown",
            notification.AttemptedAt
        );

        // Here you could also:
        // - Track failed authentication attempts for rate limiting
        // - Send security alerts for repeated failures
        // - Update fraud detection systems
        // - Log to security monitoring systems

        await Task.CompletedTask;
    }
}
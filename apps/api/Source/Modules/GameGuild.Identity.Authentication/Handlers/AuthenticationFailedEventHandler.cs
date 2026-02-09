using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for logging authentication failure events and performing security actions
/// </summary>
public sealed class AuthenticationFailedEventHandler(ILogger<AuthenticationFailedEventHandler> logger) : INotificationHandler<AuthenticationFailedEvent>
{
    public async Task Handle(AuthenticationFailedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Authentication failed for identifier {Identifier}. Reason: {Reason}. IP: {IpAddress}, User Agent: {UserAgent}, Time: {Timestamp}",
            notification.Identifier,
            notification.Reason,
            notification.IpAddress ?? "Unknown",
            notification.UserAgent ?? "Unknown",
            notification.Timestamp
        );

        // Here you could also:
        // - Track failed authentication attempts for rate limiting
        // - Send security alerts for repeated failures
        // - Update fraud detection systems
        // - Log to security monitoring systems
        // - Trigger account lockout after threshold reached
        // - Send email notifications for suspicious activity

        await Task.CompletedTask;
    }
}

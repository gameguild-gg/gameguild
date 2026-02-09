using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for user signed up notifications - sends welcome email.
///     PLANNED: Inject IEmailService (SendGrid, SMTP, etc.) when implemented.
/// </summary>
public sealed class SendWelcomeEmailHandler(ILogger<SendWelcomeEmailHandler> logger) : INotificationHandler<UserSignedUpNotification>
{
    public Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken)
    {
        // PLANNED: Replace with actual email service call:
        // await _emailService.SendWelcomeEmailAsync(notification.Email, notification.Username);
        logger.LogInformation("Welcome email requested for user {Email} (ID: {UserId}) — no email service configured",
            notification.Email, notification.UserId);

        return Task.CompletedTask;
    }
}

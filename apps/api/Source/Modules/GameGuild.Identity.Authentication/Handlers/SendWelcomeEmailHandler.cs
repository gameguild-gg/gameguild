using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for user signed up notifications - sends welcome email
/// </summary>
public class SendWelcomeEmailHandler(ILogger<SendWelcomeEmailHandler> logger) : INotificationHandler<UserSignedUpNotification>
{
    // TODO: Inject IEmailService when implemented

    public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken)
    {
        // In a real application, you would send an actual email
        logger.LogInformation("Sending welcome email to user {Email} (ID: {UserId})", notification.Email, notification.UserId);

        // TODO: Replace with actual email service call
        // await _emailService.SendWelcomeEmailAsync(notification.Email, notification.Username);

        // Simulate email sending delay
        await Task.Delay(100, cancellationToken);

        logger.LogInformation("Welcome email sent to {Email}", notification.Email);
    }
}

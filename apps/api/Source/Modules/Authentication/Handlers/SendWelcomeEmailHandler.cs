using MediatR;


namespace GameGuild.Modules.Auth;

/// <summary>
/// Handler for user signed up notifications - sends welcome email
/// </summary>
public class SendWelcomeEmailHandler(ILogger<SendWelcomeEmailHandler> logger) : INotificationHandler<UserSignedUpNotification> {
  public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken) {
    // In a real application, you would send an actual email
    logger.LogInformation(
      "Sending welcome email to user {Email} (ID: {UserId})",
      notification.Email,
      notification.UserId
    );

    // Simulate email sending delay
    await Task.Delay(100, cancellationToken);

    logger.LogInformation("Welcome email sent to {Email}", notification.Email);
  }
}

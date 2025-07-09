using GameGuild.Modules.Authentication.Notifications;
using MediatR;


namespace GameGuild.Modules.Authentication.Handlers;

/// <summary>
/// Handler for user signed up notifications - logs analytics event
/// </summary>
public class LogAnalyticsEventHandler(ILogger<LogAnalyticsEventHandler> logger) : INotificationHandler<UserSignedUpNotification> {
  public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken) {
    // In a real application, you would send this to an analytics service
    logger.LogInformation(
      "Analytics: User sign-up event - UserId: {UserId}, Email: {Email}, Time: {SignUpTime}",
      notification.UserId,
      notification.Email,
      notification.SignUpTime
    );

    // Simulate analytics API call
    await Task.Delay(50, cancellationToken);

    logger.LogInformation("Analytics event logged for user {Email}", notification.Email);
  }
}

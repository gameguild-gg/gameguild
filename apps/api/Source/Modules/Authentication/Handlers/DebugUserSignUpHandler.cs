using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Debug handler to verify UserSignedUpNotification events are working
/// </summary>
public class DebugUserSignUpHandler(ILogger<DebugUserSignUpHandler> logger)
  : INotificationHandler<UserSignedUpNotification> {
  public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken) {
    logger.LogInformation("🎉 DEBUG: New user registration detected!");
    logger.LogInformation("User ID: {UserId}", notification.UserId);
    logger.LogInformation("Email: {Email}", notification.Email);
    logger.LogInformation("Username: {Username}", notification.Username);
    logger.LogInformation("Tenant ID: {TenantId}", notification.TenantId);
    logger.LogInformation("Sign-up Time: {SignUpTime}", notification.SignUpTime);

    // Add any debug logic here
    await Task.Delay(100, cancellationToken); // Simulate processing

    logger.LogInformation("✅ DEBUG: Event processing completed for user {UserId}", notification.UserId);
  }
}

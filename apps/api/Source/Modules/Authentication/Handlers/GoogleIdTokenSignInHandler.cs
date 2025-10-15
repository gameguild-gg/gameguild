namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for Google ID token sign-in command
/// </summary>
public class GoogleIdTokenSignInHandler(
  IAuthService authService,
  IMediator mediator,
  ILogger<GoogleIdTokenSignInHandler> logger
) : IRequestHandler<GoogleIdTokenSignInCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(GoogleIdTokenSignInCommand request, CancellationToken cancellationToken) {
    var signInRequest = new GoogleIdTokenRequestDto { IdToken = request.IdToken, TenantId = request.TenantId };

    var result = await authService.GoogleIdTokenSignInAsync(signInRequest);

    // For now, we'll check if a UserProfile exists to determine if this was a new user
    // This is a simple heuristic - if no profile exists, we assume it's a new user
    if (result?.User != null) {
      try {
        // Always publish the notification - the UserProfile handler will check if profile already exists
        var notification = new UserSignedUpNotification {
          UserId = result.User.Id,
          Email = result.User.Email,
          Username = result.User.Username ?? result.User.Email,
          TenantId = request.TenantId,
          SignUpTime = DateTime.UtcNow,
        };

        await mediator.Publish(notification, cancellationToken);

        logger.LogInformation("Published UserSignedUpNotification for Google OAuth user {UserId}", result.User.Id);
      }
      catch (Exception ex) {
        logger.LogWarning(ex, "Failed to publish UserSignedUpNotification for Google OAuth user {UserId}", result.User.Id);
        // Don't fail the sign-in process if notification fails
      }
    }

    return result ?? throw new InvalidOperationException("Authentication service returned null result");
  }
}

using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for Google ID token sign-in command </summary>
public class GoogleIdTokenSignInHandler(IAuthService authService, IMediator mediator, ILogger<GoogleIdTokenSignInHandler> logger) : IRequestHandler<GoogleIdTokenSignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(GoogleIdTokenSignInCommand request, CancellationToken cancellationToken)
    {
        var signInRequest = new GoogleIdTokenRequestDto { IdToken = request.IdToken, TenantId = request.TenantId };

        var result = await authService.GoogleIdTokenSignInAsync(signInRequest);

        if (result?.User == null) return result ?? throw new InvalidOperationException("Authentication service returned null result");

        try
        {
            // Publish sign-in event
            await mediator.Publish(new UserSignedInEvent(
                result.User.Id,
                result.User.Email,
                "Google",
                null, // IP address would need to be passed from context
                null, // User agent would need to be passed from context
                DateTime.UtcNow
            ), cancellationToken);

            // For now, we'll check if a UserProfile exists to determine if this was a new user
            // This is a simple heuristic - if no profile exists, we assume it's a new user
            // Always publish the notification - the handler will check if it's actually a new user
            var notification = new UserSignedUpNotification
            {
                UserId = result.User.Id,
                Email = result.User.Email,
                Username = result.User.Username ?? result.User.Email,
                TenantId = request.TenantId
            };

            await mediator.Publish(notification, cancellationToken);

            logger.LogInformation("Published authentication events for Google OAuth user {UserId}", result.User.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish authentication events for Google OAuth user {UserId}", result.User.Id);
            // Don't fail the sign-in process if notification fails
        }

        return result ?? throw new InvalidOperationException("Authentication service returned null result");
    }
}

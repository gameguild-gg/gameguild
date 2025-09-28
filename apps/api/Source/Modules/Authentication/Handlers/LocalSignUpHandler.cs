using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for local sign-up command </summary>
public class LocalSignUpHandler(IAuthService authService, IMediator mediator, ILogger<LocalSignUpHandler> logger) : IRequestHandler<LocalSignUpCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(LocalSignUpCommand request, CancellationToken cancellationToken)
    {
        var signUpRequest = new LocalSignUpRequest { Email = request.Email, Password = request.Password, Username = request.Username, TenantId = request.TenantId };

        var result = await authService.LocalSignUpAsync(signUpRequest);

        // Publish sign-up event
        await mediator.Publish(new UserSignedUpEvent(
            result.User.Id,
            result.User.Email,
            "Local",
            DateTime.UtcNow
        ), cancellationToken);

        // Publish notification for side effects (email, analytics, etc.)
        var notification = new UserSignedUpNotification
        {
            UserId = result.User.Id,
            Email = result.User.Email,
            Username = request.Username,
            TenantId = result.TenantId
        };

        await mediator.Publish(notification, cancellationToken);

        logger.LogInformation("User {UserId} successfully signed up via local authentication", result.User.Id);

        return result;
    }
}

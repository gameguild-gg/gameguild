using MediatR;
using GameGuild.Modules.Auth.Commands;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.Notifications;
using GameGuild.Modules.Auth.Services;


namespace GameGuild.Modules.Auth.Handlers;

/// <summary>
/// Handler for local sign-up command
/// </summary>
public class LocalSignUpHandler(IAuthService authService, IMediator mediator) : IRequestHandler<LocalSignUpCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(LocalSignUpCommand request, CancellationToken cancellationToken) {
    var signUpRequest = new LocalSignUpRequestDto { Email = request.Email, Password = request.Password, Username = request.Username, TenantId = request.TenantId };

    var result = await authService.LocalSignUpAsync(signUpRequest);

    // Publish notification for side effects (email, analytics, etc.)
    var notification = new UserSignedUpNotification {
      UserId = result.User.Id, Email = result.User.Email, Username = request.Username, TenantId = result.TenantId, // Include the tenant ID from the response
    };

    await mediator.Publish(notification, cancellationToken);

    return result;
  }
}

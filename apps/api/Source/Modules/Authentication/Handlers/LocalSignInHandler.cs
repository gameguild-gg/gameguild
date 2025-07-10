using MediatR;


namespace GameGuild.Modules.Auth;

/// <summary>
/// Handler for local sign-in command
/// </summary>
public class LocalSignInHandler(IAuthService authService) : IRequestHandler<LocalSignInCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(LocalSignInCommand request, CancellationToken cancellationToken) {
    var signInRequest = new LocalSignInRequestDto { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

    return await authService.LocalSignInAsync(signInRequest);
  }
}

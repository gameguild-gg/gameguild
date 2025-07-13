using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for Google ID token sign-in command
/// </summary>
public class GoogleIdTokenSignInHandler(IAuthService authService) : IRequestHandler<GoogleIdTokenSignInCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(GoogleIdTokenSignInCommand request, CancellationToken cancellationToken) {
    var signInRequest = new GoogleIdTokenRequestDto { IdToken = request.IdToken, TenantId = request.TenantId };

    return await authService.GoogleIdTokenSignInAsync(signInRequest);
  }
}

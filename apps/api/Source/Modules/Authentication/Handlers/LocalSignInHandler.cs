using GameGuild.Modules.Authentication.Commands;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using MediatR;


namespace GameGuild.Modules.Authentication.Handlers;

/// <summary>
/// Handler for local sign-in command
/// </summary>
public class LocalSignInHandler(IAuthService authService) : IRequestHandler<LocalSignInCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(LocalSignInCommand request, CancellationToken cancellationToken) {
    var signInRequest = new LocalSignInRequestDto { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

    return await authService.LocalSignInAsync(signInRequest);
  }
}

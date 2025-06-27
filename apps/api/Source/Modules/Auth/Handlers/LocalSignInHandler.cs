using MediatR;
using GameGuild.Modules.Auth.Commands;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.Services;


namespace GameGuild.Modules.Auth.Handlers;

/// <summary>
/// Handler for local sign-in command
/// </summary>
public class LocalSignInHandler(IAuthService authService) : IRequestHandler<LocalSignInCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(LocalSignInCommand request, CancellationToken cancellationToken) {
    var signInRequest = new LocalSignInRequestDto { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

    return await authService.LocalSignInAsync(signInRequest);
  }
}

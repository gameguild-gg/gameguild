using GameGuild.Modules.Authentication.Dtos;
using MediatR;


namespace GameGuild.Modules.Authentication.Commands;

/// <summary>
/// Command to handle local user sign-in
/// </summary>
public class LocalSignInCommand : IRequest<SignInResponseDto> {
  public string Email { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public Guid? TenantId { get; set; }
}

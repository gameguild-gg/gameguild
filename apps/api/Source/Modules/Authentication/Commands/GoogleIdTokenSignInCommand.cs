using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to handle Google ID token sign-in (for NextAuth.js integration)
/// </summary>
public class GoogleIdTokenSignInCommand : IRequest<SignInResponseDto> {
  public string IdToken { get; set; } = string.Empty;

  public Guid? TenantId { get; set; }
}

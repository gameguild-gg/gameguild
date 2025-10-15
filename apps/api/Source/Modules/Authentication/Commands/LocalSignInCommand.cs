namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to handle local user sign-in
/// </summary>
public class LocalSignInCommand : IRequest<SignInResponseDto> {
  public string Email { get; init; } = string.Empty;

  public string Password { get; init; } = string.Empty;

  public Guid? TenantId { get; init; }
}

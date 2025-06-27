using MediatR;
using GameGuild.Modules.Auth.Dtos;


namespace GameGuild.Modules.Auth.Commands;

/// <summary>
/// Command to handle local user sign-in
/// </summary>
public class LocalSignInCommand : IRequest<SignInResponseDto> {
  private string _email = string.Empty;

  private string _password = string.Empty;

  private Guid? _tenantId;

  public string Email {
    get => _email;
    set => _email = value;
  }

  public string Password {
    get => _password;
    set => _password = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }
}

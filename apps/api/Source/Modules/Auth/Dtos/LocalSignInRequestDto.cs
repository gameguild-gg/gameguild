using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Auth.Dtos {
  public class LocalSignInRequestDto {
    private string? _username;

    private string _email = string.Empty;

    private string _password = string.Empty;

    private Guid? _tenantId;

    public string? Username {
      get => _username;
      set => _username = value;
    }

    [Required, EmailAddress]
    public string Email {
      get => _email;
      set => _email = value;
    }

    [Required]
    public string Password {
      get => _password;
      set => _password = value;
    }

    /// <summary>
    /// Optional tenant ID to use for the sign-in
    /// If not provided, will use the first available tenant for the user
    /// </summary>
    public Guid? TenantId {
      get => _tenantId;
      set => _tenantId = value;
    }
  }
}

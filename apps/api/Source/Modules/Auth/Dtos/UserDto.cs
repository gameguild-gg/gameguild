namespace GameGuild.Modules.Auth.Dtos {
  /// <summary>
  /// DTO for user information
  /// </summary>
  public class UserDto {
    private Guid _id;

    private string _username = string.Empty;

    private string _email = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id {
      get => _id;
      set => _id = value;
    }

    /// <summary>
    /// Username
    /// </summary>
    public string Username {
      get => _username;
      set => _username = value;
    }

    /// <summary>
    /// Email address
    /// </summary>
    public string Email {
      get => _email;
      set => _email = value;
    }
  }
}

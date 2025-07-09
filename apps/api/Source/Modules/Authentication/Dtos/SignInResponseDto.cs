using System.Text.Json.Serialization;


namespace GameGuild.Modules.Authentication.Dtos {
  /// <summary>
  /// DTO for sign-in response
  /// </summary>
  public class SignInResponseDto {
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime Expires { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserDto User { get; set; } = new UserDto();

    /// <summary>
    /// Current tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// List of tenants the user has access to
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<TenantInfoDto>? AvailableTenants { get; set; }
  }
}

using System.Text.Json.Serialization;


namespace GameGuild.Modules.Authentication {
  /// <summary>
  /// DTO for sign-in response
  /// </summary>
  public class SignInResponseDto {
    /// <summary>
    /// JWT access token
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token
    /// </summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    [JsonPropertyName("user")]
    public UserDto User { get; set; } = new UserDto();

    /// <summary>
    /// Current tenant ID
    /// </summary>
    [JsonPropertyName("tenantId")]
    public Guid? TenantId { get; set; }

    /// <summary>
    /// List of tenants the user has access to
    /// </summary>
    [JsonPropertyName("availableTenants")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<TenantInfoDto>? AvailableTenants { get; set; }
  }
}

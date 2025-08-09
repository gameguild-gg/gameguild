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
  /// Backward compatible field: originally represented refresh token expiry (or conflated); prefer using AccessTokenExpiresAt / RefreshTokenExpiresAt.
  /// </summary>
  [JsonPropertyName("expiresAt")]
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// When the access token expires (short lived)
  /// </summary>
  [JsonPropertyName("accessTokenExpiresAt")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public DateTime AccessTokenExpiresAt { get; set; }

  /// <summary>
  /// When the refresh token expires (long lived)
  /// </summary>
  [JsonPropertyName("refreshTokenExpiresAt")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public DateTime RefreshTokenExpiresAt { get; set; }

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

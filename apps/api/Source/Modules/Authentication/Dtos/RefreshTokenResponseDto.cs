namespace GameGuild.Modules.Auth;

/// <summary>
/// Response DTO with refreshed authentication tokens
/// </summary>
public class RefreshTokenResponseDto {
  /// <summary>
  /// New JWT access token
  /// </summary>
  public string AccessToken { get; set; } = string.Empty;

  /// <summary>
  /// New refresh token
  /// </summary>
  public string RefreshToken { get; set; } = string.Empty;

  /// <summary>
  /// When the refresh token expires
  /// </summary>
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// Tenant ID associated with this token (if any)
  /// </summary>
  public Guid? TenantId { get; set; }
}

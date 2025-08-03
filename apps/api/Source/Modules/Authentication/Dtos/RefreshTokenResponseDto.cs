namespace GameGuild.Modules.Authentication;

/// <summary>
/// Response DTO with refreshed authentication tokens
/// </summary>
public class RefreshTokenResponseDto {
  /// <summary>
  /// New JWT access token
  /// </summary>
  public string AccessToken { get; init; } = string.Empty;

  /// <summary>
  /// New refresh token
  /// </summary>
  public string RefreshToken { get; init; } = string.Empty;

  /// <summary>
  /// When the refresh token expires
  /// </summary>
  public DateTime ExpiresAt { get; init; }

  /// <summary>
  /// Tenant ID associated with this token (if any)
  /// </summary>
  public Guid? TenantId { get; init; }
}

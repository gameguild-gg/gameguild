using System.Text.Json.Serialization;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Response DTO with refreshed authentication tokens
/// </summary>
public class RefreshTokenResponseDto {
  /// <summary>
  /// New JWT access token
  /// </summary>
  [JsonPropertyName("accessToken")]
  public string AccessToken { get; init; } = string.Empty;

  /// <summary>
  /// New refresh token
  /// </summary>
  [JsonPropertyName("refreshToken")]
  public string RefreshToken { get; init; } = string.Empty;

  /// <summary>
  /// When the refresh token expires
  /// </summary>
  [JsonPropertyName("expiresAt")]
  public DateTime ExpiresAt { get; init; }

  /// <summary>
  /// Tenant ID associated with this token (if any)
  /// </summary>
  [JsonPropertyName("tenantId")]
  public Guid? TenantId { get; init; }
}

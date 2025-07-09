namespace GameGuild.Modules.Authentication.Dtos {
  /// <summary>
  /// Request DTO for refreshing authentication tokens
  /// </summary>
  public class RefreshTokenRequestDto {
    /// <summary>
    /// The refresh token to use
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID to generate tenant-specific claims
    /// If not specified, no tenant claims will be included
    /// </summary>
    public Guid? TenantId { get; set; }
  }

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
}

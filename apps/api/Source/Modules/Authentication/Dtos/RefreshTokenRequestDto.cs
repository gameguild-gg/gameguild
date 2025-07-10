namespace GameGuild.Modules.Authentication {
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
}

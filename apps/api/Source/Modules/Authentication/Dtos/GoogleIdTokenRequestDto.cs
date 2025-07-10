namespace GameGuild.Modules.Auth;

/// <summary>
/// Request DTO for Google ID Token validation (NextAuth.js integration)
/// </summary>
public class GoogleIdTokenRequestDto {
  /// <summary>
  /// Google ID Token from NextAuth.js
  /// </summary>
  public string IdToken { get; set; } = string.Empty;

  /// <summary>
  /// Optional tenant ID to use for the sign-in
  /// If not provided, will use the first available tenant for the user
  /// </summary>
  public Guid? TenantId { get; set; }
}

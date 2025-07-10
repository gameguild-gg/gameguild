namespace GameGuild.Modules.Auth {
  /// <summary>
  /// Request DTO for OAuth sign-in
  /// </summary>
  public class OAuthSignInRequestDto {
    /// <summary>
    /// OAuth authorization code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URI used in the OAuth flow
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID to use for the sign-in
    /// If not provided, will use the first available tenant for the user
    /// </summary>
    public Guid? TenantId { get; set; }
  }
}

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Request DTO for Google social sign-in
/// </summary>
public class GoogleSignInRequest
{
    /// <summary>
    /// Google access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }
}

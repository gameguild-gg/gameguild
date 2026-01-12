namespace GameGuild.Identity.Authentication;

/// <summary>
/// Request DTO for GitHub social sign-in
/// </summary>
public class GitHubSignInRequest
{
    /// <summary>
    /// GitHub access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }
}

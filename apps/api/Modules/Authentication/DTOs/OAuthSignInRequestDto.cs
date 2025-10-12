namespace GameGuild.Modules.Authentication;

/// <summary>
/// DTO for OAuth sign-in requests
/// </summary>
public class OAuthSignInRequestDto
{
    public string Provider { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }
}

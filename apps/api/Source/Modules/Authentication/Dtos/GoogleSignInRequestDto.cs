namespace GameGuild.Modules.Authentication;

/// <summary>
/// DTO for Google sign-in requests
/// </summary>
public class GoogleSignInRequestDto
{
    public string IdToken { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }
}

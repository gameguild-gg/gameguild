namespace GameGuild.Identity.Authentication;

/// <summary>
///     MFA verification response
/// </summary>
public class MfaVerificationResponse
{
    public bool IsValid { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }
}

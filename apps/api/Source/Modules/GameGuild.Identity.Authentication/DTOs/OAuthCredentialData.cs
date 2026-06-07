namespace GameGuild.Identity.Authentication;

/// <summary>
///     OAuth credential data
/// </summary>
public class OAuthCredentialData : ICredentialData
{
    public string Provider { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string Type { get => "oauth"; }
}

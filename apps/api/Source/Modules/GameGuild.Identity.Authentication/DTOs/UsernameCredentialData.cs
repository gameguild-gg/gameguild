namespace GameGuild.Identity.Authentication;

/// <summary>
///     Username credential data
/// </summary>
public class UsernameCredentialData : ICredentialData
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Type { get => "username"; }
}

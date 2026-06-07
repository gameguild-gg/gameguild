namespace GameGuild.Identity.Authentication;

/// <summary>
///     Email credential data
/// </summary>
public class EmailCredentialData : ICredentialData
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Type { get => "email"; }
}

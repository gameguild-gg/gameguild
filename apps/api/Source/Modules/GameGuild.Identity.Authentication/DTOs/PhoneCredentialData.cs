namespace GameGuild.Identity.Authentication;

/// <summary>
///     Phone credential data
/// </summary>
public class PhoneCredentialData : ICredentialData
{
    public string PhoneNumber { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Type { get => "phone"; }
}

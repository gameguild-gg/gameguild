namespace GameGuild.Identity.Authentication;

/// <summary>
///     MFA configuration response
/// </summary>
public class MfaConfigurationResponse
{
    public bool IsEnabled { get; set; }

    public string[ ] EnabledMethods { get; set; } = [];

    public DateTime? EnabledAt { get; set; }

    public int BackupCodesRemaining { get; set; }
}

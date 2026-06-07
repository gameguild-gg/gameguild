namespace GameGuild.Identity.Authentication;

/// <summary>
///     Backup codes response
/// </summary>
public class BackupCodesResponse
{
    public string[ ] Codes { get; set; } = [];

    public DateTime GeneratedAt { get; set; }
}

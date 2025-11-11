namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Backup codes response
/// </summary>
public class BackupCodesResponse
{
    public string[ ] Codes { get; set; } = [];

    public DateTime GeneratedAt { get; set; }
}

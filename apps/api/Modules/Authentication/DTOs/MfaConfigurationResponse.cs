namespace GameGuild.Modules.Authentication;

public class MfaConfigurationResponse
{
    public bool IsEnabled { get; set; }

    public List<MfaMethod> AvailableMethods { get; set; } = [];

    public int BackupCodesRemaining { get; set; }

    public DateTime? LastUsedAt { get; set; }
}

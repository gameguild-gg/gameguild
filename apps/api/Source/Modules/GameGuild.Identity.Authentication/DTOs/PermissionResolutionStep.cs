namespace GameGuild.Identity.Authentication;

public abstract class PermissionResolutionStep
{
    public string Level { get; set; } = string.Empty;

    public bool Found { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}

namespace GameGuild.Identity.Authentication;

public abstract class PermissionTrend
{
    public DateTime Date { get; set; }

    public int PermissionCount { get; set; }

    public int UserCount { get; set; }
}

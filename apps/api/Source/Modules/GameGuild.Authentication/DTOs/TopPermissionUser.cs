namespace GameGuild.Authentication.DTOs;

public abstract class TopPermissionUser
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int PermissionCount { get; set; }

    public DateTime LastActivity { get; set; }
}

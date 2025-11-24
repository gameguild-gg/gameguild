namespace GameGuild.Authentication.DTOs;

public abstract class BulkPermissionFailure
{
    public Guid UserId { get; set; }

    public string Error { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}

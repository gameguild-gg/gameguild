namespace GameGuild.Modules.Resources;

/// <summary> Request to share a resource with users </summary>
public class ShareResourceRequest
{
    public string[ ] UserEmails { get; set; } = [];

    public Guid[ ] UserIds { get; set; } = [];

    public PermissionType[ ] Permissions { get; set; } = [];

    public DateTime? ExpiresAt { get; set; }

    public string? Message { get; set; }

    public bool RequireAcceptance { get; set; } = true;

    public bool NotifyUsers { get; set; } = true;
}

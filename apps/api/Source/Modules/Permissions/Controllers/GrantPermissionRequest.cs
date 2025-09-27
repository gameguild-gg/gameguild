namespace GameGuild.Modules.Permissions.Controllers;

public class GrantPermissionRequest {
    public Guid? TenantId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
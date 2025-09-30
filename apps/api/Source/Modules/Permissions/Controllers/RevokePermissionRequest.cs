namespace GameGuild.Modules.Permissions.Controllers;

public class RevokePermissionRequest
{
    public Guid? TenantId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }
}

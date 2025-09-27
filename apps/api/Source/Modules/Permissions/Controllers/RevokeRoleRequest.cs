namespace GameGuild.Modules.Permissions.Controllers;

public class RevokeRoleRequest {
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public ModuleType Module { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
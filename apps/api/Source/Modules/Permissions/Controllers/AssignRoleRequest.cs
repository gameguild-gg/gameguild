namespace GameGuild.Modules.Permissions.Controllers;

public class AssignRoleRequest {
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public ModuleType Module { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public List<PermissionConstraint>? Constraints { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
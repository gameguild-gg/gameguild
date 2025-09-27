namespace GameGuild.Modules.Permissions.Controllers;

public class AdminAssignRoleRequest {
    public Guid? TenantId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}
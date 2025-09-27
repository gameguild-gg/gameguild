namespace GameGuild.Modules.Permissions.Controllers;

public class CreateRoleRequest {
    public string RoleName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<ModulePermissionDefinition> Permissions { get; set; } = [];

    public int Priority { get; set; } = 0;
}
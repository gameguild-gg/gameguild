namespace GameGuild.Modules.Permissions.Controllers;

public class UpdateRoleRequest {
    public List<ModulePermissionDefinition> Permissions { get; set; } = [];
}
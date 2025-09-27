namespace GameGuild.Modules.Permissions.Controllers;

public class CreateRoleTemplateRequest {
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<PermissionTemplate> PermissionTemplates { get; set; } = [];
}
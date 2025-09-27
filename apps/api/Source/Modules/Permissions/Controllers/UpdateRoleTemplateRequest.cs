namespace GameGuild.Modules.Permissions.Controllers;

public class UpdateRoleTemplateRequest {
    public string Description { get; set; } = string.Empty;

    public List<PermissionTemplate> PermissionTemplates { get; set; } = [];
}
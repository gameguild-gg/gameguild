namespace GameGuild.Modules.Permissions;

/// <summary> Simple permission template - defines what action can be performed on what resource type </summary>
public class PermissionTemplate
{
    public string Action { get; set; } = string.Empty; // "read", "create", "edit", "delete", etc.

    public string ResourceType { get; set; } = string.Empty; // "TestingSession", "Project", etc.

    public List<PermissionConstraint> Constraints { get; set; } = [];
}
namespace GameGuild.Modules.Permissions;

/// <summary> Module permission definition </summary>
public class ModulePermissionDefinition
{
    public ModuleType Module { get; set; }

    public ModuleAction Action { get; set; }

    public List<PermissionConstraint> Constraints { get; set; } = [];

    public bool IsGranted { get; set; } = true;

    public DateTime? ExpiresAt { get; set; }
}

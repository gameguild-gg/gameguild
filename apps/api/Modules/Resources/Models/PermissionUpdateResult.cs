namespace GameGuild.Modules.Resources;

/// <summary> Result of updating user permissions </summary>
public class PermissionUpdateResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public PermissionType[ ] GrantedPermissions { get; set; } = [];

    public PermissionType[ ] RevokedPermissions { get; set; } = [];
}

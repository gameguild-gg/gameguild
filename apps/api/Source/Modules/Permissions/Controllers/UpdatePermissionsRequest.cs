namespace GameGuild.Controllers;

/// <summary> Request to update user permissions </summary>
public class UpdatePermissionsRequest {
    public PermissionType[] Permissions { get; set; } = [];

    public DateTime? ExpiresAt { get; set; }
}
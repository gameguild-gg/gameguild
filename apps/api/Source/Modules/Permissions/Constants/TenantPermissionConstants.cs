namespace GameGuild.Modules.Permissions.Constants;

/// <summary>
/// Constants defining tenant-level permission sets and policies
/// </summary>
public static class TenantPermissionConstants
{
    /// <summary>
    /// Minimal permissions required for basic user functionality
    /// </summary>
    public static readonly PermissionType[ ] MinimalUserPermissions = [PermissionType.Read, PermissionType.Comment];

    /// <summary>
    /// Standard user permissions for active participation
    /// </summary>
    public static readonly PermissionType[ ] StandardUserPermissions =
    [
        PermissionType.Read, PermissionType.Create, PermissionType.Comment, PermissionType.Delete // Only own content
    ];

    /// <summary>
    /// Moderator permissions for content management
    /// </summary>
    public static readonly PermissionType[ ] ModeratorPermissions = [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete, PermissionType.Report];

    /// <summary>
    /// Administrator permissions for tenant management
    /// </summary>
    public static readonly PermissionType[ ] AdminPermissions = [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete, PermissionType.Report, PermissionType.TenantAdmin];
}

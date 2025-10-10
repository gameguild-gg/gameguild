using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to get all permissions for a user across all layers (tenant, content-type, resource)
/// </summary>
public class GetUserPermissionsQuery : IRequest<UserPermissionsDto>
{
    /// <summary>
    /// User ID to get permissions for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID (null for global permissions)
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Whether to include resource-level permissions
    /// </summary>
    public bool IncludeResourcePermissions { get; init; } = true;

    /// <summary>
    /// Whether to include effective permissions (resolved through hierarchy)
    /// </summary>
    public bool IncludeEffectivePermissions { get; init; } = true;
}

/// <summary>
/// DTO containing all permissions for a user
/// </summary>
public class UserPermissionsDto
{
    /// <summary>
    /// Tenant-level permissions
    /// </summary>
    public IEnumerable<PermissionType> TenantPermissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Global permissions (cross-tenant)
    /// </summary>
    public IEnumerable<PermissionType> GlobalPermissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Resource-level permissions grouped by resource
    /// </summary>
    public Dictionary<string, IEnumerable<PermissionType>> ResourcePermissions { get; set; } = new();

    /// <summary>
    /// Effective permissions (combined from all layers)
    /// </summary>
    public IEnumerable<PermissionType> EffectivePermissions { get; set; } = Array.Empty<PermissionType>();
}

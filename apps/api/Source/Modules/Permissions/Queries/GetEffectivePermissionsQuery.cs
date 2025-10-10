using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to get effective permissions for a user (resolved through all layers with precedence rules)
/// </summary>
public class GetEffectivePermissionsQuery : IRequest<EffectivePermissionsDto>
{
    /// <summary>
    /// User ID to get effective permissions for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Optional resource type for resource-specific effective permissions
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Optional resource ID for resource-specific effective permissions
    /// </summary>
    public Guid? ResourceId { get; init; }

    /// <summary>
    /// Whether to include permission source information (which layer granted each permission)
    /// </summary>
    public bool IncludeSource { get; init; } = true;
}

/// <summary>
/// DTO containing effective permissions with source information
/// </summary>
public class EffectivePermissionsDto
{
    /// <summary>
    /// Effective permissions (combined from all layers with precedence applied)
    /// </summary>
    public IEnumerable<PermissionType> Permissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Permission sources (which layer granted each permission)
    /// </summary>
    public Dictionary<PermissionType, PermissionSource> PermissionSources { get; set; } = new();

    /// <summary>
    /// Whether any permissions are expired or about to expire
    /// </summary>
    public bool HasExpiringPermissions { get; set; }

    /// <summary>
    /// Expiring permissions with their expiration dates
    /// </summary>
    public Dictionary<PermissionType, DateTime> ExpiringPermissions { get; set; } = new();
}

/// <summary>
/// Source layer of a permission
/// </summary>
public enum PermissionSource
{
    Global,
    Tenant,
    ContentType,
    Resource,
    Owner
}

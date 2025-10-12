using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to get all resource permissions for a user
/// </summary>
public class GetResourcePermissionsQuery : IRequest<IEnumerable<PermissionType>>
{
    /// <summary>
    /// User ID to get permissions for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Resource type
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Resource ID
    /// </summary>
    public Guid ResourceId { get; init; }

    /// <summary>
    /// Whether to include effective permissions (resolved through hierarchy)
    /// </summary>
    public bool IncludeEffectivePermissions { get; init; } = true;
}

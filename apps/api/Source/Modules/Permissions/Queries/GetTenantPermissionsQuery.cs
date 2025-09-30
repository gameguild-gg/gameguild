using GameGuild.CQRS;
using GameGuild.Modules.Permissions;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to get all tenant permissions for a user
/// </summary>
public class GetTenantPermissionsQuery : IRequest<IEnumerable<PermissionType>>
{
    /// <summary>
    /// User ID to get permissions for
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Whether to include effective permissions (resolved through hierarchy)
    /// </summary>
    public bool IncludeEffectivePermissions { get; init; } = true;
}
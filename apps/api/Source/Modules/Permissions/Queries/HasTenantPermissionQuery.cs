using GameGuild.CQRS;
using GameGuild.Modules.Permissions;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Query to check if a user has specific tenant permission
/// </summary>
public class HasTenantPermissionQuery : IRequest<bool>
{
    /// <summary>
    /// User ID to check permission for
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Permission to check
    /// </summary>
    public PermissionType Permission { get; init; }
}
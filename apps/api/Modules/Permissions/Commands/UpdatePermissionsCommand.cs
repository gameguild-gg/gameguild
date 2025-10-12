using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to update permissions in bulk (add/remove multiple permissions)
/// </summary>
public class UpdatePermissionsCommand : IRequest<Unit>
{
    /// <summary>
    /// User ID to update permissions for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID (null for global permissions)
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Permissions to grant
    /// </summary>
    public PermissionType[] PermissionsToGrant { get; init; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Permissions to revoke
    /// </summary>
    public PermissionType[] PermissionsToRevoke { get; init; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Optional reason for permission changes
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Optional expiration date for granted permissions
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

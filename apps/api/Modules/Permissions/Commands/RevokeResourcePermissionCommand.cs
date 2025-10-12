using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to revoke resource-level permissions from a user
/// </summary>
public class RevokeResourcePermissionCommand : IRequest<Unit>
{
    /// <summary>
    /// User ID to revoke permissions from
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
    /// Permissions to revoke
    /// </summary>
    public PermissionType[] Permissions { get; init; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Optional reason for revoking permissions
    /// </summary>
    public string? Reason { get; init; }
}

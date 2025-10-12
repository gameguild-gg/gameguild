using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to revoke tenant permissions from a user
/// </summary>
public class RevokeTenantPermissionCommand : IRequest<Unit>
{
    /// <summary>
    /// User ID to revoke permissions from
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Permissions to revoke
    /// </summary>
    public PermissionType[] Permissions { get; init; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Optional reason for revoking permissions
    /// </summary>
    public string? Reason { get; init; }
}
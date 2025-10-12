using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to revoke global permissions from a user
/// </summary>
public class RevokeGlobalPermissionCommand : IRequest<Unit>
{
    /// <summary>
    /// User ID to revoke permissions from
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Permissions to revoke globally
    /// </summary>
    public PermissionType[] Permissions { get; init; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Optional reason for revoking permissions
    /// </summary>
    public string? Reason { get; init; }
}

using GameGuild.CQRS;
using GameGuild.Modules.Resources;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to grant resource-level permissions to a user
/// </summary>
public class GrantResourcePermissionCommand : IRequest<ResourcePermission<EntityBase>>
{
    /// <summary>
    /// User ID to grant permissions to
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Resource type (e.g., "Course", "Project", "Post")
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Resource ID
    /// </summary>
    public Guid ResourceId { get; init; }

    /// <summary>
    /// Permissions to grant
    /// </summary>
    public PermissionType[] Permissions { get; init; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Optional reason for granting permissions
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Optional expiration date for permissions
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

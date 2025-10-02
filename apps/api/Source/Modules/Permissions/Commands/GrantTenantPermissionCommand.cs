using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to grant tenant permissions to a user
/// </summary>
public class GrantTenantPermissionCommand : IRequest<TenantPermission>
{
    /// <summary>
    /// User ID to grant permissions to (null for default permissions)
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Tenant ID (null for global defaults)
    /// </summary>
    public Guid? TenantId { get; init; }

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
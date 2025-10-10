using GameGuild.CQRS;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Command to grant global permissions (cross-tenant) to a user
/// </summary>
public class GrantGlobalPermissionCommand : IRequest<TenantPermission>
{
    /// <summary>
    /// User ID to grant permissions to
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Permissions to grant globally (across all tenants)
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

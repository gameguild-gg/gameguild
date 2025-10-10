using GameGuild.Core.Entities;
using GameGuild.Modules.Permissions;

namespace GameGuild.Modules.Users.Entities;

/// <summary>
///     Represents a role that can be assigned to users
/// </summary>
public sealed class Role : EntityBase<Guid>
{
    /// <summary>
    ///     Role name (unique)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Role description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Display name for UI
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Whether this role is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    ///     Whether this is a system-defined role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; init; }

    /// <summary>
    ///     Permissions granted by this role
    /// </summary>
    public PermissionType[] Permissions { get; init; } = [];

    /// <summary>
    ///     Navigation property for user-role assignments
    /// </summary>
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();

    /// <summary>
    ///     Activates the role
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    ///     Deactivates the role
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    ///     Checks if role has a specific permission
    /// </summary>
    public bool HasPermission(PermissionType permission)
    {
        return Permissions.Contains(permission);
    }
}

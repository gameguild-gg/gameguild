using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Users;

/// <summary>
///     Repository interface for UserRole junction entity
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    ///     Assigns a role to a user
    /// </summary>
    Task<UserRole> AssignAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Assigns multiple roles to multiple users in bulk
    /// </summary>
    Task<List<UserRole>> AssignBulkAsync(IEnumerable<UserRole> userRoles, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Unassigns a role from a user
    /// </summary>
    Task UnassignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all roles assigned to a user
    /// </summary>
    Task<List<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all users with a specific role
    /// </summary>
    Task<List<UserRole>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has a specific role
    /// </summary>
    Task<bool> HasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific user-role assignment
    /// </summary>
    Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes expired role assignments
    /// </summary>
    Task RemoveExpiredAsync(CancellationToken cancellationToken = default);
}

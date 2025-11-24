using GameGuild.Authentication.Entities;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Repository for managing Role entities
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    ///     Get a role by its ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role if found, null otherwise</returns>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all roles, optionally filtered by tenant
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="includeInactive">Whether to include inactive roles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    Task<List<Role>> GetAllAsync(Guid? tenantId = null, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a role by its name within a tenant
    /// </summary>
    /// <param name="name">Role name</param>
    /// <param name="tenantId">Tenant ID (null for global roles)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role if found, null otherwise</returns>
    Task<Role?> GetByNameAsync(string name, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Add a new role
    /// </summary>
    /// <param name="role">Role to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created role</returns>
    Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing role
    /// </summary>
    /// <param name="role">Role to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a role by its ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a role with the given name exists within a tenant
    /// </summary>
    /// <param name="name">Role name</param>
    /// <param name="tenantId">Tenant ID (null for global roles)</param>
    /// <param name="excludeRoleId">Optional role ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if role exists</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? tenantId = null, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all roles assigned to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeExpired">Whether to include expired role assignments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    Task<List<Role>> GetUserRolesAsync(Guid userId, bool includeExpired = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Assign a role to a user
    /// </summary>
    /// <param name="userRole">User-role assignment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user-role assignment</returns>
    Task<UserRole> AssignRoleToUserAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Remove a role from a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a user has a specific role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has the role</returns>
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

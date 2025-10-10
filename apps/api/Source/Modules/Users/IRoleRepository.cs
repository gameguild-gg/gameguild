using GameGuild.Modules.Users;

namespace GameGuild.Modules.Users;

/// <summary>
///     Repository interface for Role entity
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    ///     Gets a role by ID
    /// </summary>
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets multiple roles by IDs
    /// </summary>
    Task<List<Role>> GetByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a role by name
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active roles
    /// </summary>
    Task<List<Role>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a role exists by name
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if multiple roles exist
    /// </summary>
    Task<bool> ExistAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a new role
    /// </summary>
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing role
    /// </summary>
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a role (if not system role)
    /// </summary>
    Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default);
}

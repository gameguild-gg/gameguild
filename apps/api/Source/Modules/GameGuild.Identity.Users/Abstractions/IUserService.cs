
namespace GameGuild.Identity.Users;

/// <summary>
///     Service interface for user business operations
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Creates a new user
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <param name="phoneNumber">User phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user</returns>
    Task<User> CreateUserAsync(string email, string name, string? phoneNumber = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="name">New name</param>
    /// <param name="phoneNumber">New phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user</returns>
    Task<User> UpdateUserAsync(Guid userId, string name, string? phoneNumber = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Activates a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activated user</returns>
    Task<User> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deactivates a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deactivated user</returns>
    Task<User> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a user (soft delete)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a user by email
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Searches users
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    Task<(IEnumerable<UserDto> Users, int TotalCount)> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records user activity (last seen)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordUserActivityAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if an email is already in use
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <param name="excludeUserId">User ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email is in use, false otherwise</returns>
    Task<bool> IsEmailInUseAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}

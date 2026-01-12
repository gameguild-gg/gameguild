
namespace GameGuild.Identity.Users;

/// <summary>
///     User repository interface for managing user data operations
/// </summary>
public interface IUserRepository
{
    /// <summary>
    ///     Gets a user by their unique identifier
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a user by their email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all users</returns>
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Searches users based on search criteria
    /// </summary>
    /// <param name="searchTerm">Search term to match against name or email</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing users and total count</returns>
    Task<(IEnumerable<User> Users, int TotalCount)> SearchAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new user
    /// </summary>
    /// <param name="user">User to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing user
    /// </summary>
    /// <param name="user">User to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a user
    /// </summary>
    /// <param name="user">User to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves changes to the underlying data store
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Bulk Operations

    /// <summary>
    ///     Gets multiple users by their unique identifiers
    /// </summary>
    /// <param name="ids">Collection of user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of found users</returns>
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets multiple users by their email addresses
    /// </summary>
    /// <param name="emails">Collection of email addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of found users</returns>
    Task<IEnumerable<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds multiple users
    /// </summary>
    /// <param name="users">Collection of users to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates multiple users
    /// </summary>
    /// <param name="users">Collection of users to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes multiple users
    /// </summary>
    /// <param name="users">Collection of users to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets active users only
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active users</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets inactive users only
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of inactive users</returns>
    Task<IEnumerable<User>> GetInactiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets users with pagination and filtering
    /// </summary>
    /// <param name="isActive">Filter by active status (null for all)</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing users and total count</returns>
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if users exist by their email addresses
    /// </summary>
    /// <param name="emails">Collection of email addresses to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping email addresses to existence status</returns>
    Task<IDictionary<string, bool>> CheckEmailsExistAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Permanently deletes a user from the database (hard delete)
    /// </summary>
    /// <param name="user">User to permanently delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PurgeAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Permanently deletes multiple users from the database (hard delete)
    /// </summary>
    /// <param name="users">Collection of users to permanently delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PurgeRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a queryable collection of users for advanced filtering
    /// </summary>
    /// <returns>IQueryable of users</returns>
    IQueryable<User> GetQueryable();
}

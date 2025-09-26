namespace GameGuild.Modules.Users;

/// <summary>
/// Repository interface for user data access operations
/// Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get user by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID with credentials included
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User with credentials or null if not found</returns>
    Task<User?> GetByIdWithCredentialsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID with option to include deleted users
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="includeDeleted">Whether to include soft-deleted users</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User or null if not found</returns>
    Task<User?> GetByIdAsync(Guid id, bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users with optional deleted inclusion
    /// </summary>
    /// <param name="includeDeleted">Whether to include soft-deleted users</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    Task<IEnumerable<User>> GetAllAsync(bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get only soft-deleted users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of soft-deleted users</returns>
    Task<IEnumerable<User>> GetDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search users by name or email
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="includeDeleted">Whether to include soft-deleted users</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching users</returns>
    Task<IEnumerable<User>> SearchAsync(string searchTerm, bool includeDeleted = false, CancellationToken cancellationToken = default);



    /// <summary>
    /// Get user statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User statistics</returns>
    Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if username exists
    /// </summary>
    /// <param name="username">Username to check</param>
    /// <param name="excludeUserId">User ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if username exists</returns>
    Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email exists
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <param name="excludeUserId">User ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists</returns>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get usernames starting with a prefix (for unique username generation)
    /// </summary>
    /// <param name="prefix">Username prefix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of usernames starting with prefix</returns>
    Task<IEnumerable<string>> GetUsernamesStartingWithAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if activated successfully</returns>
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deactivated successfully</returns>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations for multiple users
    /// </summary>
    Task<int> BulkActivateAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<int> BulkDeactivateAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<int> BulkSoftDeleteAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<int> BulkRestoreAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<int> BulkHardDeleteAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository interface for authentication attempt data access operations
/// </summary>
public interface IAuthenticationAttemptRepository
{
    /// <summary>
    /// Get authentication attempt by ID
    /// </summary>
    /// <param name="id">The attempt ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The authentication attempt or null if not found</returns>
    Task<AuthenticationAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get authentication attempts for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="limit">Maximum number of attempts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of authentication attempts</returns>
    Task<IReadOnlyList<AuthenticationAttempt>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get authentication attempts by email
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="limit">Maximum number of attempts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of authentication attempts</returns>
    Task<IReadOnlyList<AuthenticationAttempt>> GetByEmailAsync(string email, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get authentication attempts by IP address
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of authentication attempts</returns>
    Task<IReadOnlyList<AuthenticationAttempt>> GetByIpAddressAsync(string ipAddress, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get suspicious authentication attempts
    /// </summary>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suspicious authentication attempts</returns>
    Task<IReadOnlyList<AuthenticationAttempt>> GetSuspiciousAttemptsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new authentication attempt
    /// </summary>
    /// <param name="attempt">The authentication attempt to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created authentication attempt</returns>
    Task<AuthenticationAttempt> CreateAsync(AuthenticationAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing authentication attempt
    /// </summary>
    /// <param name="attempt">The authentication attempt to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated authentication attempt</returns>
    Task<AuthenticationAttempt> UpdateAsync(AuthenticationAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an authentication attempt
    /// </summary>
    /// <param name="id">The attempt ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count failed attempts for an email in a time window
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="fromDate">Start date for the time window</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of failed attempts</returns>
    Task<int> CountFailedAttemptsAsync(string email, DateTime fromDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count failed attempts for an IP address in a time window
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    /// <param name="fromDate">Start date for the time window</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of failed attempts</returns>
    Task<int> CountFailedAttemptsByIpAsync(string ipAddress, DateTime fromDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old authentication attempts
    /// </summary>
    /// <param name="olderThan">Delete attempts older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted attempts</returns>
    Task<int> CleanupOldAttemptsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
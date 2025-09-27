namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository interface for MFA attempt data access operations
/// </summary>
public interface IMfaAttemptRepository
{
    /// <summary>
    /// Get MFA attempt by ID
    /// </summary>
    /// <param name="id">The attempt ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA attempt or null if not found</returns>
    Task<MfaAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get MFA attempts for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="limit">Maximum number of attempts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of MFA attempts</returns>
    Task<IReadOnlyList<MfaAttempt>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get MFA attempts by method
    /// </summary>
    /// <param name="method">The MFA method</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of MFA attempts</returns>
    Task<IReadOnlyList<MfaAttempt>> GetByMethodAsync(MfaMethod method, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed MFA attempts for a user in a time window
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="fromDate">Start date for the time window</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of failed MFA attempts</returns>
    Task<IReadOnlyList<MfaAttempt>> GetFailedAttemptsByUserAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new MFA attempt
    /// </summary>
    /// <param name="attempt">The MFA attempt to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created MFA attempt</returns>
    Task<MfaAttempt> CreateAsync(MfaAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing MFA attempt
    /// </summary>
    /// <param name="attempt">The MFA attempt to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated MFA attempt</returns>
    Task<MfaAttempt> UpdateAsync(MfaAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an MFA attempt
    /// </summary>
    /// <param name="id">The attempt ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count failed MFA attempts for a user in a time window
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="fromDate">Start date for the time window</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of failed attempts</returns>
    Task<int> CountFailedAttemptsAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old MFA attempts
    /// </summary>
    /// <param name="olderThan">Delete attempts older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted attempts</returns>
    Task<int> CleanupOldAttemptsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository for managing authentication attempt records.
///     Stores all login attempts for security monitoring and anomaly detection.
/// </summary>
public interface IAuthenticationAttemptRepository
{
    /// <summary>
    ///     Records a new authentication attempt.
    /// </summary>
    Task<AuthenticationAttempt> CreateAsync(AuthenticationAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an authentication attempt by ID.
    /// </summary>
    Task<AuthenticationAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all authentication attempts for a specific user.
    /// </summary>
    Task<List<AuthenticationAttempt>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets failed authentication attempts for a specific identifier within a time window.
    /// </summary>
    Task<List<AuthenticationAttempt>> GetFailedAttemptsAsync(string identifier, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts failed password sign-in attempts for a normalized account identifier
    ///     (email) since the given UTC timestamp. Used by the progressive sign-in
    ///     backoff (issue #1084); the count is per account, not per source IP.
    /// </summary>
    Task<int> CountRecentFailedAttemptsAsync(string email, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets suspicious authentication attempts for analysis.
    /// </summary>
    Task<List<AuthenticationAttempt>> GetSuspiciousAttemptsAsync(DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the most recent successful authentication for a user.
    /// </summary>
    Task<AuthenticationAttempt?> GetLastSuccessfulAttemptAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets recent authentication attempts for a user within a specified time window.
    ///     Used for anomaly detection and behavioral analysis.
    /// </summary>
    Task<List<AuthenticationAttempt>> GetRecentAttemptsAsync(Guid userId, DateTime since, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets recent authentication attempts by IP address within a specified time window.
    ///     Used for IP-based anomaly detection.
    /// </summary>
    Task<List<AuthenticationAttempt>> GetRecentAttemptsByIpAsync(string ipAddress, DateTime since, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets authentication attempts statistics for a user.
    /// </summary>
    Task<(int TotalAttempts, int SuccessfulAttempts, int FailedAttempts)> GetUserStatisticsAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes old authentication attempts (for data retention policies).
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}

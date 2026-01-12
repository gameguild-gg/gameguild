namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository for managing MFA verification attempts.
///     Tracks MFA challenges and their outcomes for security monitoring.
/// </summary>
public interface IMfaAttemptRepository
{
    /// <summary>
    ///     Records a new MFA attempt.
    /// </summary>
    Task<MfaAttempt> CreateAsync(MfaAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an MFA attempt by ID.
    /// </summary>
    Task<MfaAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets MFA attempts for a specific user.
    /// </summary>
    Task<List<MfaAttempt>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets failed MFA attempts within a time window (for lockout detection).
    /// </summary>
    Task<List<MfaAttempt>> GetFailedAttemptsAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts failed MFA attempts for a user within a time window.
    /// </summary>
    Task<int> CountFailedAttemptsAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes old MFA attempts (for data retention policies).
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}

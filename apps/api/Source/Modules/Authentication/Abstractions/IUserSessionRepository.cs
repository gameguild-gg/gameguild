namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository interface for user session data access operations
/// </summary>
public interface IUserSessionRepository
{
    /// <summary>
    /// Get user session by ID
    /// </summary>
    /// <param name="id">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user session or null if not found</returns>
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user session by refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user session or null if not found</returns>
    Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active sessions for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active user sessions</returns>
    Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all sessions for a user (including inactive)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all user sessions</returns>
    Task<IReadOnlyList<UserSession>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sessions by device fingerprint
    /// </summary>
    /// <param name="deviceFingerprint">The device fingerprint</param>
    /// <param name="activeOnly">Whether to return only active sessions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user sessions</returns>
    Task<IReadOnlyList<UserSession>> GetByDeviceFingerprintAsync(string deviceFingerprint, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new user session
    /// </summary>
    /// <param name="session">The user session to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user session</returns>
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing user session
    /// </summary>
    /// <param name="session">The user session to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated user session</returns>
    Task<UserSession> UpdateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last used time for a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> UpdateLastUsedAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminate a user session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="reason">Reason for termination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if terminated, false if not found</returns>
    Task<bool> TerminateSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminate all sessions for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="reason">Reason for termination</param>
    /// <param name="excludeSessionId">Optional session ID to exclude from termination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions terminated</returns>
    Task<int> TerminateAllUserSessionsAsync(Guid userId, string reason, Guid? excludeSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user session
    /// </summary>
    /// <param name="id">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark device as trusted in session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> MarkDeviceAsTrustedAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

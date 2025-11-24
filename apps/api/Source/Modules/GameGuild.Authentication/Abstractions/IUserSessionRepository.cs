using GameGuild.Authentication.Entities;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Repository for managing user sessions.
///     Stores active session information, device details, and session lifecycle.
/// </summary>
public interface IUserSessionRepository
{
    /// <summary>
    ///     Creates a new user session.
    /// </summary>
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a session by ID.
    /// </summary>
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a session by refresh token.
    /// </summary>
    Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all sessions for a specific user.
    /// </summary>
    Task<List<UserSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active sessions for a user.
    /// </summary>
    Task<List<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing session (for last used date, etc.).
    /// </summary>
    Task<UserSession> UpdateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Terminates a specific session.
    /// </summary>
    Task TerminateAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Terminates all sessions for a user.
    /// </summary>
    Task TerminateAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Terminates all sessions except the specified one (for "logout other sessions").
    /// </summary>
    Task TerminateAllExceptAsync(Guid userId, Guid keepSessionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes expired sessions (cleanup task).
    /// </summary>
    Task DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts active sessions for a user.
    /// </summary>
    Task<int> CountActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

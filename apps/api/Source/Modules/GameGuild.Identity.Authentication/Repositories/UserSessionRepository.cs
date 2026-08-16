using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for user session data access operations
/// </summary>
public class UserSessionRepository(IApplicationDbContext context) : IUserSessionRepository
{
    private DbSet<UserSession> UserSessions { get => context.Set<UserSession>(); }

    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await UserSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken); }

    public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await UserSessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<List<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await UserSessions.Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > SystemClock.UtcNow).OrderByDescending(s => s.LastUsedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<UserSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await UserSessions.Where(s => s.UserId == userId).OrderByDescending(s => s.LastUsedAt).ToListAsync(cancellationToken);
    }

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();
        session.UpdatedAt = SystemClock.UtcNow;
        session.LastUsedAt = SystemClock.UtcNow;

        UserSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }

    public async Task<UserSession> UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        session.UpdatedAt = SystemClock.UtcNow;

        UserSessions.Update(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }

    public async Task TerminateAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session == null) return;

        session.IsActive = false;
        session.TerminationReason = reason;
        session.TerminatedAt = SystemClock.UtcNow;
        session.UpdatedAt = SystemClock.UtcNow;

        await UpdateAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public async Task TerminateAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var activeSessions = await UserSessions.Where(s => s.UserId == userId && s.IsActive).ToListAsync(cancellationToken);

        if (activeSessions.Count == 0) return;

        var now = SystemClock.UtcNow;

        foreach (var session in activeSessions)
        {
            session.IsActive = false;
            session.TerminationReason = reason;
            session.TerminatedAt = now;
            session.UpdatedAt = now;
        }

        UserSessions.UpdateRange(activeSessions);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task TerminateAllExceptAsync(Guid userId, Guid keepSessionId, string reason, CancellationToken cancellationToken = default)
    {
        var activeSessions = await UserSessions.Where(s => s.UserId == userId && s.IsActive && s.Id != keepSessionId).ToListAsync(cancellationToken);

        if (activeSessions.Count == 0) return;

        var now = SystemClock.UtcNow;

        foreach (var session in activeSessions)
        {
            session.IsActive = false;
            session.TerminationReason = reason;
            session.TerminatedAt = now;
            session.UpdatedAt = now;
        }

        UserSessions.UpdateRange(activeSessions);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var expiredSessions = await UserSessions.Where(s => s.ExpiresAt < now || !s.IsActive && s.TerminatedAt.HasValue && s.TerminatedAt.Value.AddDays(30) < now).ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0) return;

        UserSessions.RemoveRange(expiredSessions);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;

        return await UserSessions.CountAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > now, cancellationToken);
    }

    // Helper methods for backward compatibility
    public async Task<IReadOnlyList<UserSession>> GetByDeviceFingerprintAsync(string deviceFingerprint, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = UserSessions.Where(s => s.DeviceFingerprint == deviceFingerprint);

        if (activeOnly) query = query.Where(s => s.IsActive && s.ExpiresAt > SystemClock.UtcNow);

        return await query.OrderByDescending(s => s.LastUsedAt).ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateLastUsedAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session is not { IsActive: true }) return false;

        session.LastUsedAt = SystemClock.UtcNow;
        session.UpdatedAt = SystemClock.UtcNow;

        await UpdateAsync(session, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<bool> MarkDeviceAsTrustedAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (session == null) return false;

        session.IsTrustedDevice = true;
        session.TrustedAt = SystemClock.UtcNow;
        session.UpdatedAt = SystemClock.UtcNow;

        await UpdateAsync(session, cancellationToken).ConfigureAwait(false);

        return true;
    }
}

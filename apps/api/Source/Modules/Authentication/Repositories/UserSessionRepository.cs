using GameGuild.Database;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository implementation for user session data access operations
/// </summary>
public class UserSessionRepository(ApplicationDbContext context) : IUserSessionRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken); }

    public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions.Where(s => s.UserId == userId && s.IsValid).OrderByDescending(s => s.LastUsedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions.Where(s => s.UserId == userId).OrderByDescending(s => s.LastUsedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetByDeviceFingerprintAsync(string deviceFingerprint, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        IQueryable<UserSession> query = _context.UserSessions.Where(s => s.DeviceFingerprint == deviceFingerprint);

        if (activeOnly) query = query.Where(s => s.IsValid);

        return await query.OrderByDescending(s => s.LastUsedAt).ToListAsync(cancellationToken);
    }

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        session.Id = Guid.NewGuid();
        session.CreatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.LastUsedAt = DateTime.UtcNow;

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return session;
    }

    public async Task<UserSession> UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        session.UpdatedAt = DateTime.UtcNow;

        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);

        return session;
    }

    public async Task<bool> UpdateLastUsedAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        UserSession? session = await GetByIdAsync(sessionId, cancellationToken);

        if (session == null || !session.IsValid) return false;

        session.LastUsedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(session, cancellationToken);

        return true;
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        UserSession? session = await GetByIdAsync(sessionId, cancellationToken);

        if (session == null) return false;

        session.IsActive = false;
        session.TerminationReason = reason;
        session.TerminatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(session, cancellationToken);

        return true;
    }

    public async Task<int> TerminateAllUserSessionsAsync(Guid userId, string reason, Guid? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        List<UserSession> activeSessions = await _context.UserSessions.Where(s => s.UserId == userId && s.IsActive && (excludeSessionId == null || s.Id != excludeSessionId.Value)).ToListAsync(cancellationToken);

        if (activeSessions.Count == 0) return 0;

        DateTime now = DateTime.UtcNow;

        foreach (UserSession session in activeSessions)
        {
            session.IsActive = false;
            session.TerminationReason = reason;
            session.TerminatedAt = now;
            session.UpdatedAt = now;
        }

        _context.UserSessions.UpdateRange(activeSessions);
        await _context.SaveChangesAsync(cancellationToken);

        return activeSessions.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserSession? session = await GetByIdAsync(id, cancellationToken);

        if (session == null) return false;

        _context.UserSessions.Remove(session);
        int changes = await _context.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }

    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        List<UserSession> expiredSessions = await _context.UserSessions.Where(s => s.ExpiresAt < now || (!s.IsActive && s.TerminatedAt.HasValue && s.TerminatedAt.Value.AddDays(30) < now)).ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0) return 0;

        _context.UserSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredSessions.Count;
    }

    public async Task<bool> MarkDeviceAsTrustedAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        UserSession? session = await GetByIdAsync(sessionId, cancellationToken);

        if (session == null) return false;

        session.IsTrustedDevice = true;
        session.TrustedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await UpdateAsync(session, cancellationToken);

        return true;
    }
}

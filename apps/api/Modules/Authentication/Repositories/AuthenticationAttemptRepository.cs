using GameGuild.Database;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository implementation for authentication attempt data access operations
/// </summary>
public class AuthenticationAttemptRepository(ApplicationDbContext context) : IAuthenticationAttemptRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<AuthenticationAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.AuthenticationAttempts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken); }

    public async Task<IReadOnlyList<AuthenticationAttempt>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuthenticationAttempts.Where(a => a.UserId == userId).OrderByDescending(a => a.AttemptedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthenticationAttempt>> GetByEmailAsync(string email, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuthenticationAttempts.Where(a => a.Email == email.ToLowerInvariant()).OrderByDescending(a => a.AttemptedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthenticationAttempt>> GetByIpAddressAsync(string ipAddress, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<AuthenticationAttempt> query = _context.AuthenticationAttempts.Where(a => a.IpAddress == ipAddress);

        if (fromDate.HasValue) query = query.Where(a => a.AttemptedAt >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(a => a.AttemptedAt <= toDate.Value);

        return await query.OrderByDescending(a => a.AttemptedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthenticationAttempt>> GetSuspiciousAttemptsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<AuthenticationAttempt> query = _context.AuthenticationAttempts.Where(a => a.IsSuspicious || a.RiskScore > 70);

        if (fromDate.HasValue) query = query.Where(a => a.AttemptedAt >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(a => a.AttemptedAt <= toDate.Value);

        return await query.OrderByDescending(a => a.AttemptedAt).ToListAsync(cancellationToken);
    }

    public async Task<AuthenticationAttempt> CreateAsync(AuthenticationAttempt attempt, CancellationToken cancellationToken = default)
    {
        attempt.Id = Guid.NewGuid();
        attempt.CreatedAt = DateTime.UtcNow;
        attempt.UpdatedAt = DateTime.UtcNow;
        attempt.Email = attempt.Email.ToLowerInvariant();

        _context.AuthenticationAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return attempt;
    }

    public async Task<AuthenticationAttempt> UpdateAsync(AuthenticationAttempt attempt, CancellationToken cancellationToken = default)
    {
        attempt.UpdatedAt = DateTime.UtcNow;

        _context.AuthenticationAttempts.Update(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return attempt;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AuthenticationAttempt? attempt = await GetByIdAsync(id, cancellationToken);

        if (attempt == null) return false;

        _context.AuthenticationAttempts.Remove(attempt);
        int changes = await _context.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }

    public async Task<int> CountFailedAttemptsAsync(string email, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.AuthenticationAttempts.Where(a => a.Email == email.ToLowerInvariant() && !a.IsSuccessful && a.AttemptedAt >= fromDate).CountAsync(cancellationToken);
    }

    public async Task<int> CountFailedAttemptsByIpAsync(string ipAddress, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.AuthenticationAttempts.Where(a => a.IpAddress == ipAddress && !a.IsSuccessful && a.AttemptedAt >= fromDate).CountAsync(cancellationToken);
    }

    public async Task<int> CleanupOldAttemptsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        List<AuthenticationAttempt> oldAttempts = await _context.AuthenticationAttempts.Where(a => a.AttemptedAt < olderThan).ToListAsync(cancellationToken);

        if (oldAttempts.Count == 0) return 0;

        _context.AuthenticationAttempts.RemoveRange(oldAttempts);
        await _context.SaveChangesAsync(cancellationToken);

        return oldAttempts.Count;
    }
}

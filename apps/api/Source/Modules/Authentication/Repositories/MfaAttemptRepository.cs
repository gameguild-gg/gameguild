using GameGuild.Database;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository implementation for MFA attempt data access operations
/// </summary>
public class MfaAttemptRepository(ApplicationDbContext context) : IMfaAttemptRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<MfaAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.MfaAttempts.FirstOrDefaultAsync(m => m.Id == id, cancellationToken); }

    public async Task<IReadOnlyList<MfaAttempt>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.MfaAttempts.Where(m => m.UserId == userId).OrderByDescending(m => m.CreatedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MfaAttempt>> GetByMethodAsync(MfaMethod method, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        IQueryable<MfaAttempt> query = _context.MfaAttempts.Where(m => m.Method == method);

        if (fromDate.HasValue) query = query.Where(m => m.CreatedAt >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(m => m.CreatedAt <= toDate.Value);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MfaAttempt>> GetFailedAttemptsByUserAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.MfaAttempts.Where(m => m.UserId == userId && !m.IsSuccessful && m.CreatedAt >= fromDate).OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<MfaAttempt> CreateAsync(MfaAttempt attempt, CancellationToken cancellationToken = default)
    {
        attempt.Id = Guid.NewGuid();
        attempt.CreatedAt = DateTime.UtcNow;
        attempt.UpdatedAt = DateTime.UtcNow;

        _context.MfaAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return attempt;
    }

    public async Task<MfaAttempt> UpdateAsync(MfaAttempt attempt, CancellationToken cancellationToken = default)
    {
        attempt.UpdatedAt = DateTime.UtcNow;

        _context.MfaAttempts.Update(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return attempt;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        MfaAttempt? attempt = await GetByIdAsync(id, cancellationToken);

        if (attempt == null) return false;

        _context.MfaAttempts.Remove(attempt);
        int changes = await _context.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }

    public async Task<int> CountFailedAttemptsAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.MfaAttempts.Where(m => m.UserId == userId && !m.IsSuccessful && m.CreatedAt >= fromDate).CountAsync(cancellationToken);
    }

    public async Task<int> CleanupOldAttemptsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        List<MfaAttempt> oldAttempts = await _context.MfaAttempts.Where(m => m.CreatedAt < olderThan).ToListAsync(cancellationToken);

        if (oldAttempts.Count == 0) return 0;

        _context.MfaAttempts.RemoveRange(oldAttempts);
        await _context.SaveChangesAsync(cancellationToken);

        return oldAttempts.Count;
    }
}

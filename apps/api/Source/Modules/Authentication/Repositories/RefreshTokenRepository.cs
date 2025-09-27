using GameGuild.Database;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository implementation for refresh token data access operations
/// </summary>
public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.Id = Guid.NewGuid();
        refreshToken.CreatedAt = DateTime.UtcNow;
        refreshToken.UpdatedAt = DateTime.UtcNow;

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.UpdatedAt = DateTime.UtcNow;

        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<bool> RevokeTokenAsync(string token, string revokedByIp, string? replacedByToken = null, CancellationToken cancellationToken = default)
    {
        RefreshToken? refreshToken = await GetByTokenAsync(token, cancellationToken);
        if (refreshToken == null || refreshToken.IsRevoked)
            return false;

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByToken = replacedByToken;

        await UpdateAsync(refreshToken, cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllUserTokensAsync(Guid userId, string revokedByIp, CancellationToken cancellationToken = default)
    {
        List<RefreshToken> activeTokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.IsActive)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
            return 0;

        DateTime now = DateTime.UtcNow;
        foreach (RefreshToken token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
            token.UpdatedAt = now;
        }

        _context.RefreshTokens.UpdateRange(activeTokens);
        await _context.SaveChangesAsync(cancellationToken);

        return activeTokens.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RefreshToken? refreshToken = await GetByIdAsync(id, cancellationToken);
        if (refreshToken == null)
            return false;

        _context.RefreshTokens.Remove(refreshToken);
        int changes = await _context.SaveChangesAsync(cancellationToken);

        return changes > 0;
    }

    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        List<RefreshToken> expiredTokens = await _context.RefreshTokens
            .Where(r => (r.IsRevoked && r.RevokedAt.HasValue && r.RevokedAt.Value.AddDays(30) < now) ||
                       (!r.IsRevoked && r.ExpiresAt < now.AddDays(-30)))
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count == 0)
            return 0;

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredTokens.Count;
    }
}
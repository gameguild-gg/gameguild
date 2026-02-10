using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for refresh token data access operations
/// </summary>
public class RefreshTokenRepository(IApplicationDbContext context) : IRefreshTokenRepository
{
    private DbSet<RefreshToken> RefreshTokens { get => context.Set<RefreshToken>(); }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) { return await RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, cancellationToken); }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await RefreshTokens.Where(r => r.UserId == userId && r.IsActive).OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await RefreshTokens.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.Id = Guid.NewGuid();
        refreshToken.UpdatedAt = SystemClock.UtcNow;

        RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return refreshToken;
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.UpdatedAt = SystemClock.UtcNow;

        RefreshTokens.Update(refreshToken);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return refreshToken;
    }

    public async Task RevokeAsync(string token, string? revokedByIp = null, string? replacedByToken = null, CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetByTokenAsync(token, cancellationToken).ConfigureAwait(false);

        if (refreshToken == null || refreshToken.IsRevoked) return;

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = SystemClock.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByToken = replacedByToken;
        refreshToken.UpdatedAt = SystemClock.UtcNow;

        await UpdateAsync(refreshToken, cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var activeTokens = await RefreshTokens.Where(r => r.UserId == userId && r.IsActive).ToListAsync(cancellationToken);

        if (activeTokens.Count == 0) return;

        var now = SystemClock.UtcNow;

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevokedByIp = revokedByIp;
            token.UpdatedAt = now;
        }

        RefreshTokens.UpdateRange(activeTokens);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteExpiredAndRevokedAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var expiredTokens = await RefreshTokens.Where(r => r.IsRevoked && r.RevokedAt.HasValue && r.RevokedAt.Value < cutoffDate || !r.IsRevoked && r.ExpiresAt < cutoffDate).ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            RefreshTokens.RemoveRange(expiredTokens);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await RefreshTokens.FirstOrDefaultAsync(r => r.Id == id, cancellationToken); }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (refreshToken == null) return false;

        RefreshTokens.Remove(refreshToken);
        var changes = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return changes > 0;
    }
}

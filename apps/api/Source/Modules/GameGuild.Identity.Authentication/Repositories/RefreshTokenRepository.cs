using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for refresh token data access operations
/// </summary>
public class RefreshTokenRepository(IApplicationDbContext context) : IRefreshTokenRepository, IRefreshTokenLineageRepository
{
    private DbSet<RefreshToken> RefreshTokens { get => context.Set<RefreshToken>(); }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) { return await RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, cancellationToken); }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        return await RefreshTokens.Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > now)
            .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task<bool> BindSessionAsync(Guid userId, string tokenHash, Guid sessionId, CancellationToken cancellationToken)
    {
        RequireIdentity(userId, sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        var token = await RefreshTokens.AsNoTracking().SingleOrDefaultAsync(value => value.Token == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (token is null)
        {
            return false;
        }
        var session = await context.Set<UserSession>().AsNoTracking().SingleOrDefaultAsync(value => value.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);
        if (token.UserId != userId || token.IsRevoked || token.ExpiresAt <= SystemClock.UtcNow ||
            session is null || session.UserId != userId ||
            session.RefreshToken != tokenHash || !session.IsActive || session.ExpiresAt <= SystemClock.UtcNow ||
            token.SessionId.HasValue && token.SessionId.Value != sessionId)
        {
            throw new UnauthorizedAccessException("Invalid refresh-token session binding");
        }
        var tokenLink = TrackForMetadata(token);
        tokenLink.SessionId = sessionId;
        tokenLink.UpdatedAt = SystemClock.UtcNow;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task RecordRotationAsync(Guid userId, Guid parentTokenId, string replacementTokenHash, Guid sessionId,
        CancellationToken cancellationToken)
    {
        RequireIdentity(userId, sessionId);
        if (parentTokenId == Guid.Empty)
        {
            throw new ArgumentException("A persisted predecessor is required.", nameof(parentTokenId));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementTokenHash);
        // ExecuteUpdate claims bypass the tracker; validate the committed-in-transaction row, not its stale tracked copy.
        var parent = await RefreshTokens.AsNoTracking().SingleOrDefaultAsync(value => value.Id == parentTokenId, cancellationToken)
            .ConfigureAwait(false);
        var child = await RefreshTokens.AsNoTracking().SingleOrDefaultAsync(value => value.Token == replacementTokenHash, cancellationToken)
            .ConfigureAwait(false);
        var session = await context.Set<UserSession>().AsNoTracking().SingleOrDefaultAsync(value => value.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);
        if (parent is null || child is null || session is null || parent.Id == child.Id ||
            parent.UserId != userId || child.UserId != userId || session.UserId != userId ||
            !parent.IsRevoked || parent.ReplacedByToken != replacementTokenHash ||
            child.IsRevoked || child.ExpiresAt <= SystemClock.UtcNow ||
            session.RefreshToken != replacementTokenHash || !session.IsActive || session.ExpiresAt <= SystemClock.UtcNow ||
            parent.SessionId.HasValue && parent.SessionId.Value != sessionId ||
            child.SessionId.HasValue && child.SessionId.Value != sessionId ||
            child.ParentTokenId.HasValue && child.ParentTokenId.Value != parentTokenId)
        {
            throw new UnauthorizedAccessException("Invalid refresh-token lineage");
        }
        // A legacy predecessor can be bound only by this observed successful rotation and owned session.
        var parentLink = TrackForMetadata(parent);
        var childLink = TrackForMetadata(child);
        parentLink.SessionId = sessionId;
        childLink.SessionId = sessionId;
        childLink.ParentTokenId = parent.Id;
        parentLink.UpdatedAt = childLink.UpdatedAt = SystemClock.UtcNow;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private RefreshToken TrackForMetadata(RefreshToken stored)
    {
        var tracked = RefreshTokens.Local.SingleOrDefault(value => value.Id == stored.Id);
        if (tracked is not null)
        {
            return tracked;
        }
        RefreshTokens.Attach(stored);
        return stored;
    }

    private static void RequireIdentity(Guid userId, Guid sessionId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A persisted user ID is required.", nameof(userId));
        }
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("A persisted session ID is required.", nameof(sessionId));
        }
    }

    public async Task<bool> TryRevokeForRotationAsync(
        Guid tokenId,
        string expectedTokenHash,
        string replacementTokenHash,
        DateTime revokedAt,
        string? revokedByIp,
        CancellationToken cancellationToken)
    {
        if (tokenId == Guid.Empty)
        {
            throw new ArgumentException("Refresh token ID is required.", nameof(tokenId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementTokenHash);

        var affected = await RefreshTokens
            .Where(token =>
                token.Id == tokenId &&
                token.Token == expectedTokenHash &&
                !token.IsRevoked &&
                token.ReplacedByToken == null &&
                token.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.IsRevoked, true)
                .SetProperty(token => token.RevokedAt, revokedAt)
                .SetProperty(token => token.RevokedByIp, revokedByIp)
                .SetProperty(token => token.ReplacedByToken, replacementTokenHash)
                .SetProperty(token => token.UpdatedAt, revokedAt), cancellationToken)
            .ConfigureAwait(false);

        return affected == 1;
    }

    public async Task RevokeAsync(string token, string? revokedByIp = null, string? replacedByToken = null, CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetByTokenAsync(token, cancellationToken).ConfigureAwait(false);

        if (refreshToken == null || refreshToken.IsRevoked)
        {
            return;
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = SystemClock.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByToken = replacedByToken;
        refreshToken.UpdatedAt = SystemClock.UtcNow;

        await UpdateAsync(refreshToken, cancellationToken).ConfigureAwait(false);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var activeTokens = await RefreshTokens.Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > now)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (activeTokens.Count == 0)
        {
            return;
        }

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
        // Delete leaves first. Retain predecessors while any child is retained, including an active descendant.
        while (true)
        {
            var expiredTokens = await RefreshTokens.Where(r =>
                (r.IsRevoked && r.RevokedAt.HasValue && r.RevokedAt.Value < cutoffDate || !r.IsRevoked && r.ExpiresAt < cutoffDate) &&
                !RefreshTokens.Any(child => child.ParentTokenId == r.Id)).Take(500).ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (expiredTokens.Count == 0)
            {
                return;
            }
            RefreshTokens.RemoveRange(expiredTokens);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await RefreshTokens.FirstOrDefaultAsync(r => r.Id == id, cancellationToken); }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (refreshToken == null)
        {
            return false;
        }

        RefreshTokens.Remove(refreshToken);
        var changes = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return changes > 0;
    }
}

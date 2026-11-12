using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for API-key credential data access operations.
/// </summary>
public class ApiKeyRepository(IApplicationDbContext context) : IApiKeyRepository
{
    private DbSet<ApiKey> ApiKeys => context.Set<ApiKey>();

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return await ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiKey?> GetByIdForUserAsync(Guid keyId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await ApiKeys
            .AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<ApiKey>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await ApiKeys
            .AsNoTracking()
            .Where(k => k.UserId == userId && k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiKey> AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        ApiKeys.Add(apiKey);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return apiKey;
    }

    public async Task<ApiKey?> RevokeAsync(Guid keyId, Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var apiKey = await ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId, cancellationToken).ConfigureAwait(false);
        if (apiKey == null)
        {
            return null;
        }

        apiKey.Revoke(reason);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return apiKey;
    }

    /// <summary>
    ///     Issue #1192: revokes every key minted (directly or transitively) by
    ///     <paramref name="creatorKeyId"/>. Breadth-first over the creator linkage so a leaked
    ///     <c>api_keys:manage</c> key cannot self-perpetuate: revoking the compromised key also
    ///     kills every replacement it minted, recursively. Returns the revoked key ids (the
    ///     creator itself is not included).
    /// </summary>
    public async Task<List<ApiKey>> RevokeCreatedByCascadeAsync(Guid creatorKeyId, string reason, CancellationToken cancellationToken = default)
    {
        var revoked = new List<ApiKey>();
        var frontier = new List<Guid> { creatorKeyId };
        var visited = new HashSet<Guid> { creatorKeyId };
        while (frontier.Count > 0)
        {
            var currentFrontier = frontier;
            frontier = [];
            var children = await ApiKeys
                .Where(key => key.CreatedByApiKeyId != null && currentFrontier.Contains(key.CreatedByApiKeyId.Value) && key.IsActive)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var child in children)
            {
                if (!visited.Add(child.Id))
                {
                    continue;
                }

                child.Revoke(reason);
                revoked.Add(child);
                frontier.Add(child.Id);
            }
        }

        if (revoked.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return revoked;
    }

    public async Task RecordUsageAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        apiKey.RecordUsage();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RotateAsync(ApiKey oldKey, ApiKey newKey, DateTime? graceEndsAt, CancellationToken cancellationToken = default)
    {
        newKey.ReplacesKeyId = oldKey.Id;
        if (graceEndsAt.HasValue)
        {
            oldKey.BeginRotationGrace(graceEndsAt.Value);
        }
        else
        {
            oldKey.Revoke($"Rotated: superseded by {newKey.Id}");
        }

        ApiKeys.Add(newKey);
        // Single save: new-key issuance and old-key transition are atomic.
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task FinalizeRotationRevocationAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        if (apiKey.FinalizeRotationRevocation())
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<int> FinalizeExpiredRotationRevocationsAsync(int batchSize) =>
        FinalizeExpiredRotationRevocationsAsync(batchSize, CancellationToken.None);

    public async Task<int> FinalizeExpiredRotationRevocationsAsync(int batchSize, CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Rotation revocation batches must contain between 1 and 1000 keys.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var now = SystemClock.UtcNow;
        var keys = await ApiKeys
            .Where(key => key.IsActive && key.RevokedAt == null &&
                          key.RotationGraceEndsAt.HasValue && key.RotationGraceEndsAt.Value <= now)
            .OrderBy(key => key.RotationGraceEndsAt)
            .ThenBy(key => key.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var finalized = 0;
        foreach (var key in keys)
        {
            if (key.FinalizeRotationRevocation()) { finalized++; }
        }

        if (finalized > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return finalized;
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using GameGuild.Configuration.ApplicationLayer;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for managing JWT signing key rotation with automatic expiry and cleanup
/// </summary>
public class KeyRotationService(
    IApplicationDbContext dbContext,
    ILogger<KeyRotationService> logger,
    IJwtSigningKeyCache? signingKeyCache = null,
    IOptions<JwtOptions>? jwtOptions = null) : IKeyRotationService
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    public async Task<JwtSigningKey?> GetActiveSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<JwtSigningKey>()
            .Where(k => k.IsActive && k.ValidFrom <= SystemClock.UtcNow && k.ExpiresAt > SystemClock.UtcNow)
            .OrderByDescending(k => k.KeyVersion)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<JwtSigningKey>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        return await _dbContext.Set<JwtSigningKey>()
            .AsNoTracking()
            .Where(k => k.ValidFrom <= now && k.ExpiresAt > now)
            .OrderByDescending(k => k.KeyVersion)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JwtSigningKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<JwtSigningKey>()
            .FirstOrDefaultAsync(k => k.KeyId == keyId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JwtSigningKey> RotateKeyAsync(string reason = "scheduled", int validityDays = 90, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(validityDays);
        // A retry after an ambiguous commit must find the original rotation, not create another one.
        var rotationId = Guid.NewGuid();
        var key = await ExecuteSerializedRotationAsync(
            () => RotateKeyCoreAsync(rotationId, reason, validityDays, cancellationToken), cancellationToken).ConfigureAwait(false);
        await RefreshValidationKeyCacheAsync(cancellationToken).ConfigureAwait(false);
        return key;
    }

    private async Task<JwtSigningKey> RotateKeyCoreAsync(Guid rotationId, string reason, int validityDays, CancellationToken cancellationToken)
    {
        var completed = await _dbContext.Set<JwtSigningKey>().AsNoTracking()
            .FirstOrDefaultAsync(key => key.Id == rotationId, cancellationToken).ConfigureAwait(false);
        if (completed is not null) { return completed; }

        var activeKeys = await _dbContext.Set<JwtSigningKey>().Where(key => key.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var lastVersion = await _dbContext.Set<JwtSigningKey>().Select(key => (int?)key.KeyVersion)
            .MaxAsync(cancellationToken).ConfigureAwait(false) ?? 0;
        var validFrom = SystemClock.UtcNow;
        var next = JwtSigningKey.CreateNew(checked(lastVersion + 1), validFrom, TimeSpan.FromDays(validityDays));
        next.Id = rotationId;
        next.Activate();
        var options = jwtOptions?.Value ?? new JwtOptions();
        var overlapEnd = validFrom.AddMinutes(options.AccessTokenExpirationMinutes).AddSeconds(options.ClockSkewSeconds);
        foreach (var key in activeKeys)
        {
            // Preserve tokens issued by every live predecessor, including legacy competing keys.
            // Expired keys stay expired: rotating must never resurrect their validity window.
            if (key.ValidFrom <= validFrom && key.ExpiresAt > validFrom && key.ExpiresAt < overlapEnd)
            {
                key.ExpiresAt = overlapEnd;
            }
            key.Rotate(reason);
        }
        _dbContext.Set<JwtSigningKey>().Add(next);
        // Activation and predecessor deactivation share one SaveChanges/audit/version batch.
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("JWT signing key rotated to version {Version}", next.KeyVersion);
        return next;
    }

    public async Task<int> CleanupExpiredKeysAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = SystemClock.UtcNow.AddDays(-retentionDays);
        
        var expiredKeys = await _dbContext.Set<JwtSigningKey>()
            .Where(k => k.ExpiresAt < cutoffDate)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (expiredKeys.Count == 0)
        {
            logger.LogDebug("No expired keys to clean up");
            return 0;
        }

        _dbContext.Set<JwtSigningKey>().RemoveRange(expiredKeys);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cleaned up {Count} expired JWT signing keys older than {CutoffDate}",
            expiredKeys.Count, cutoffDate);

        await RefreshValidationKeyCacheAsync(cancellationToken).ConfigureAwait(false);

        return expiredKeys.Count;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteSerializedRotationAsync(async () =>
        {
            // Recheck only after acquiring the database-wide fence; another node may have initialized it.
            var active = await GetActiveSigningKeyAsync(cancellationToken).ConfigureAwait(false);
            return active ?? await RotateKeyCoreAsync(Guid.NewGuid(), "initialization", 90, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        await RefreshValidationKeyCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> ExecuteSerializedRotationAsync<T>(Func<Task<T>> mutation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Non-relational unit-test contexts have no distributed transaction semantics.
        if (_dbContext is not DbContext context || !context.Database.IsRelational())
        {
            return await mutation().ConfigureAwait(false);
        }
        if (context.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            throw new InvalidOperationException("Signing-key rotation requires the shared PostgreSQL context.");
        }
        if (context.Database.CurrentTransaction is { } callerTransaction)
        {
            if (callerTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.ReadCommitted)
            {
                throw new InvalidOperationException("Signing-key rotation requires read-committed transaction visibility.");
            }
            // The caller retains transaction ownership and the fence until its commit or rollback.
            await AcquireRotationFenceAsync(context, cancellationToken).ConfigureAwait(false);
            return await mutation().ConfigureAwait(false);
        }

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var snapshots = context.ChangeTracker.Entries()
                .Select(entry => new TrackedState(entry, entry.State, entry.CurrentValues.Clone(), entry.OriginalValues.Clone(),
                    entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).ToArray()))
                .ToArray();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            try
            {
                await AcquireRotationFenceAsync(context, cancellationToken).ConfigureAwait(false);
                var result = await mutation().ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
                catch (Exception error) { logger.LogError("Signing-key transaction rollback failed ({ErrorType})", error.GetType().Name); }
                RestoreTrackedState(context, snapshots);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private static Task<int> AcquireRotationFenceAsync(DbContext context, CancellationToken cancellationToken) =>
        // Fixed global namespace: signing keys are shared across tenants and application instances.
        context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7182947351024811)", cancellationToken);

    private static void RestoreTrackedState(DbContext context, IReadOnlyList<TrackedState> snapshots)
    {
        var previous = new HashSet<object>(snapshots.Select(snapshot => snapshot.Entry.Entity), ReferenceEqualityComparer.Instance);
        foreach (var entry in context.ChangeTracker.Entries().ToArray())
        {
            if (!previous.Contains(entry.Entity)) { entry.State = EntityState.Detached; }
        }
        foreach (var snapshot in snapshots)
        {
            snapshot.Entry.State = EntityState.Unchanged;
            snapshot.Entry.CurrentValues.SetValues(snapshot.CurrentValues);
            snapshot.Entry.OriginalValues.SetValues(snapshot.OriginalValues);
            snapshot.Entry.State = snapshot.State;
            if (snapshot.State == EntityState.Modified)
            {
                foreach (var property in snapshot.Entry.Properties)
                {
                    property.IsModified = snapshot.ModifiedProperties.Contains(property.Metadata.Name, StringComparer.Ordinal);
                }
            }
        }
    }

    private sealed record TrackedState(EntityEntry Entry, EntityState State, PropertyValues CurrentValues,
        PropertyValues OriginalValues, string[] ModifiedProperties);

    /// <summary>
    ///     Publishes the currently valid key set to the process-wide validation-key cache
    ///     (issue #1141). A missing cache (unit tests, static-key-only hosts) is not an
    ///     error — validation then keeps using the statically configured secret.
    /// </summary>
    private async Task RefreshValidationKeyCacheAsync(CancellationToken cancellationToken)
    {
        // A command transaction commits after its handler returns. Readers refresh from the
        // committed store on their next validation; never publish an uncommitted rotation.
        if (signingKeyCache is null || (_dbContext is DbContext context && context.Database.CurrentTransaction is not null))
        {
            return;
        }

        await signingKeyCache.RefreshAsync(this, cancellationToken).ConfigureAwait(false);
    }
}

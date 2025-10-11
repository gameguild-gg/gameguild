using System.Text.Json;
using GameGuild.Modules.Common.Idempotency.Entities;


namespace GameGuild.Modules.Common.Idempotency;

/// <summary>
/// Database-backed implementation of idempotency service
/// </summary>
internal sealed class IdempotencyService : IIdempotencyService
{
    private readonly DbContext _dbContext;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(24);

    public IdempotencyService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyResult?> GetResultAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;

        var record = await _dbContext.Set<IdempotencyRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (record == null)
            return null;

        // Check if expired
        if (record.ExpiresAt <= DateTime.UtcNow)
        {
            // Remove expired record
            _dbContext.Set<IdempotencyRecord>().Remove(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        return new IdempotencyResult
        {
            IdempotencyKey = record.IdempotencyKey,
            ResultJson = record.ResultJson,
            StatusCode = record.StatusCode,
            CreatedAt = record.CreatedAt,
            ExpiresAt = record.ExpiresAt
        };
    }

    public async Task StoreResultAsync(string idempotencyKey, object result, int statusCode, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return;

        var resultJson = JsonSerializer.Serialize(result);
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(ttl ?? _defaultTtl);

        var record = new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            ResultJson = resultJson,
            StatusCode = statusCode,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        // Use upsert pattern (PostgreSQL)
        _dbContext.Set<IdempotencyRecord>().Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Duplicate key - already exists, ignore
        }
    }

    public async Task CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _dbContext.Set<IdempotencyRecord>()
            .Where(x => x.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

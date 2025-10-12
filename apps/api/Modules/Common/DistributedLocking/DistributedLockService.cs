namespace GameGuild.Modules.Common.DistributedLocking;

/// <summary>
/// PostgreSQL advisory locks implementation of distributed lock service
/// </summary>
internal sealed class DistributedLockService : IDistributedLockService
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DistributedLockService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _acquisitionTimeout = TimeSpan.FromSeconds(10);

    public DistributedLockService(DbContext dbContext, ILogger<DistributedLockService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IDistributedLock?> AcquireLockAsync(
        string key,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        var lockId = GetLockId(key);
        var expiration = expirationTime ?? _defaultExpiration;
        var acquiredAt = DateTime.UtcNow;
        var expiresAt = acquiredAt.Add(expiration);

        try
        {
            // Try to acquire PostgreSQL advisory lock (blocking with timeout)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_acquisitionTimeout);

            // PostgreSQL advisory lock function: pg_try_advisory_lock(bigint)
            var sql = $"SELECT pg_try_advisory_lock({lockId})";
            var result = await _dbContext.Database
                .SqlQueryRaw<bool>(sql)
                .FirstOrDefaultAsync(cts.Token);

            if (!result)
            {
                _logger.LogWarning("Failed to acquire distributed lock for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Acquired distributed lock for key: {Key} (ID: {LockId})", key, lockId);

            return new DistributedLock(key, lockId, acquiredAt, expiresAt, _dbContext, _logger);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Lock acquisition timed out for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring distributed lock for key: {Key}", key);
            return null;
        }
    }

    public async Task<IDistributedLock?> TryAcquireLockAsync(
        string key,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        var lockId = GetLockId(key);
        var expiration = expirationTime ?? _defaultExpiration;
        var acquiredAt = DateTime.UtcNow;
        var expiresAt = acquiredAt.Add(expiration);

        try
        {
            // Non-blocking lock acquisition
            var sql = $"SELECT pg_try_advisory_lock({lockId})";
            var result = await _dbContext.Database
                .SqlQueryRaw<bool>(sql)
                .FirstOrDefaultAsync(cancellationToken);

            if (!result)
            {
                return null;
            }

            return new DistributedLock(key, lockId, acquiredAt, expiresAt, _dbContext, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to acquire distributed lock for key: {Key}", key);
            return null;
        }
    }

    public async Task ExecuteWithLockAsync(
        string key,
        Func<CancellationToken, Task> action,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await AcquireLockAsync(key, expirationTime, cancellationToken);

        if (lockHandle == null)
        {
            throw new InvalidOperationException($"Failed to acquire distributed lock for key: {key}");
        }

        await action(cancellationToken);
    }

    public async Task<TResult> ExecuteWithLockAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> func,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await AcquireLockAsync(key, expirationTime, cancellationToken);

        if (lockHandle == null)
        {
            throw new InvalidOperationException($"Failed to acquire distributed lock for key: {key}");
        }

        return await func(cancellationToken);
    }

    private static long GetLockId(string key)
    {
        // Convert string key to int64 for PostgreSQL advisory lock
        unchecked
        {
            long hash = 5381;
            foreach (var c in key)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return hash;
        }
    }
}

internal sealed class DistributedLock : IDistributedLock
{
    private readonly long _lockId;
    private readonly DbContext _dbContext;
    private readonly ILogger _logger;
    private bool _isReleased;

    public string Key { get; }
    public DateTime AcquiredAt { get; }
    public DateTime ExpiresAt { get; }
    public bool IsAcquired => !_isReleased && DateTime.UtcNow < ExpiresAt;

    public DistributedLock(
        string key,
        long lockId,
        DateTime acquiredAt,
        DateTime expiresAt,
        DbContext dbContext,
        ILogger logger)
    {
        Key = key;
        _lockId = lockId;
        AcquiredAt = acquiredAt;
        ExpiresAt = expiresAt;
        _dbContext = dbContext;
        _logger = logger;
        _isReleased = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isReleased)
        {
            return;
        }

        try
        {
            // Release PostgreSQL advisory lock
            var sql = $"SELECT pg_advisory_unlock({_lockId})";
            await _dbContext.Database.SqlQueryRaw<bool>(sql).FirstOrDefaultAsync();

            _isReleased = true;
            _logger.LogDebug("Released distributed lock for key: {Key} (ID: {LockId})", Key, _lockId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing distributed lock for key: {Key}", Key);
        }
    }
}

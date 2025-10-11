using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources;

/// <summary>
///     Service for managing distributed locks for quota updates
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    ///     Acquire a lock for quota update
    /// </summary>
    Task<IDisposable?> AcquireLockAsync(Guid tenantId, ResourceUsageType resourceType, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a lock exists for a quota
    /// </summary>
    Task<bool> IsLockedAsync(Guid tenantId, ResourceUsageType resourceType, CancellationToken cancellationToken = default);
}

/// <summary>
///     In-memory implementation of distributed lock service (upgrade to Redis/SQL for production)
/// </summary>
public class DistributedLockService : IDistributedLockService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private readonly ILogger<DistributedLockService> _logger;

    public DistributedLockService(ILogger<DistributedLockService> logger)
    {
        _logger = logger;
    }

    public async Task<IDisposable?> AcquireLockAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var lockKey = GetLockKey(tenantId, resourceType);
        var semaphore = Locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        _logger.LogDebug("Attempting to acquire lock for {LockKey} with timeout {Timeout}", lockKey, timeout);

        var acquired = await semaphore.WaitAsync(timeout, cancellationToken);

        if (!acquired)
        {
            _logger.LogWarning("Failed to acquire lock for {LockKey} within timeout {Timeout}", lockKey, timeout);
            return null;
        }

        _logger.LogDebug("Successfully acquired lock for {LockKey}", lockKey);

        return new LockReleaser(semaphore, lockKey, _logger);
    }

    public Task<bool> IsLockedAsync(Guid tenantId, ResourceUsageType resourceType, CancellationToken cancellationToken = default)
    {
        var lockKey = GetLockKey(tenantId, resourceType);

        if (!Locks.TryGetValue(lockKey, out var semaphore))
            return Task.FromResult(false);

        return Task.FromResult(semaphore.CurrentCount == 0);
    }

    private static string GetLockKey(Guid tenantId, ResourceUsageType resourceType)
    {
        return $"quota:{tenantId}:{resourceType}";
    }

    private class LockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly string _lockKey;
        private readonly ILogger _logger;
        private bool _disposed;

        public LockReleaser(SemaphoreSlim semaphore, string lockKey, ILogger logger)
        {
            _semaphore = semaphore;
            _lockKey = lockKey;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogDebug("Releasing lock for {LockKey}", _lockKey);
            _semaphore.Release();
            _disposed = true;
        }
    }
}

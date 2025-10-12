namespace GameGuild.Modules.Common.DistributedLocking;

/// <summary>
/// Service for acquiring and releasing distributed locks across multiple instances
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquires a distributed lock with the given key
    /// </summary>
    /// <param name="key">Unique lock key</param>
    /// <param name="expirationTime">Lock expiration time (default: 30 seconds)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock handle to release the lock</returns>
    Task<IDistributedLock?> AcquireLockAsync(
        string key,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to acquire a lock without waiting
    /// </summary>
    Task<IDistributedLock?> TryAcquireLockAsync(
        string key,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action while holding a distributed lock
    /// </summary>
    Task ExecuteWithLockAsync(
        string key,
        Func<CancellationToken, Task> action,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function while holding a distributed lock
    /// </summary>
    Task<TResult> ExecuteWithLockAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> func,
        TimeSpan? expirationTime = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a distributed lock handle
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    string Key { get; }
    DateTime AcquiredAt { get; }
    DateTime ExpiresAt { get; }
    bool IsAcquired { get; }
}

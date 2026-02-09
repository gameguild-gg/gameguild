using Microsoft.Extensions.Caching.Memory;

namespace GameGuild;

/// <summary>
///     In-memory implementation of <see cref="IIdempotencyStore"/> using <see cref="IMemoryCache"/>.
///     Suitable for single-instance deployments only.
/// </summary>
/// <remarks>
///     ⚠️ This store is local to the process — each application instance has its own cache.
///     For multi-instance deployments, replace with a distributed implementation (Redis, SQL Server).
/// </remarks>
public sealed class MemoryCacheIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _cache;
    private readonly object _inFlightLock = new();

    public MemoryCacheIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Synchronous operation wrapped for interface compliance.
    ///     <see cref="IMemoryCache"/> is inherently synchronous.
    /// </remarks>
    public Task<IdempotentResponse?> TryGetResponseAsync(string key)
    {
        _cache.TryGetValue(key, out IdempotentResponse? response);
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Synchronous operation wrapped for interface compliance.
    ///     <see cref="IMemoryCache"/> is inherently synchronous.
    /// </remarks>
    public Task SetResponseAsync(string key, IdempotentResponse response, TimeSpan duration)
    {
        _cache.Set(key, response, duration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Synchronous operation wrapped for interface compliance.
    ///     Uses a lock for atomic check-and-set within a single process.
    /// </remarks>
    public Task<bool> TryMarkInFlightAsync(string key, TimeSpan timeout)
    {
        var inFlightKey = $"{key}:in-flight";

        // Lock ensures atomic check-and-set within a single process.
        // For multi-instance deployments, use a distributed lock (e.g., RedLock).
        lock (_inFlightLock)
        {
            if (_cache.TryGetValue(inFlightKey, out _))
            {
                return Task.FromResult(false);
            }

            _cache.Set(inFlightKey, true, timeout);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Synchronous operation wrapped for interface compliance.
    ///     <see cref="IMemoryCache"/> is inherently synchronous.
    /// </remarks>
    public Task RemoveInFlightAsync(string key)
    {
        var inFlightKey = $"{key}:in-flight";
        _cache.Remove(inFlightKey);
        return Task.CompletedTask;
    }
}

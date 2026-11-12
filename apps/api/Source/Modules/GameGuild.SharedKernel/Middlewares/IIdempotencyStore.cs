namespace GameGuild;

/// <summary>
///     Abstraction for idempotency key storage.
///     Decouples the <see cref="IdempotencyMiddleware"/> from the concrete caching implementation,
///     making it easy to swap in-memory storage for a distributed cache (Redis, SQL Server, etc.)
///     without changing the middleware logic.
/// </summary>
/// <remarks>
///     <para><b>Built-in implementations:</b></para>
///     <list type="bullet">
///         <item><description><see cref="DistributedCacheIdempotencyStore"/> — <c>IDistributedCache</c>-backed (default; multi-instance safe, issue #1248 N2)</description></item>
///         <item><description><see cref="MemoryCacheIdempotencyStore"/> — in-process <c>IMemoryCache</c> (single-instance only; kept for tests and single-node hosts)</description></item>
///     </list>
///     <para>
///     Cross-instance in-flight mutual exclusion additionally requires a distributed lock
///     (e.g., RedLock) — see the concurrency caveat on
///     <see cref="DistributedCacheIdempotencyStore"/>.
///     </para>
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    ///     Tries to retrieve a cached idempotent response for the given key.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <returns>The cached response, or null if not found</returns>
    Task<IdempotentResponse?> TryGetResponseAsync(string key);

    /// <summary>
    ///     Stores an idempotent response with the configured TTL.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="response">The response to cache</param>
    /// <param name="duration">How long to keep the cached response</param>
    Task SetResponseAsync(string key, IdempotentResponse response, TimeSpan duration);

    /// <summary>
    ///     Attempts to mark a request as in-flight (being processed).
    ///     Returns false if the key is already in-flight (concurrent duplicate request).
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="timeout">How long the in-flight marker should live</param>
    /// <returns>True if the marker was set (first request); false if already in-flight</returns>
    Task<bool> TryMarkInFlightAsync(string key, TimeSpan timeout);

    /// <summary>
    ///     Removes the in-flight marker for a request (called when processing completes).
    /// </summary>
    /// <param name="key">The cache key</param>
    Task RemoveInFlightAsync(string key);
}

using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>
///     Generic helper for hybrid L1 + L2 caching with version-based invalidation.
///     Consolidates common caching logic used by cached authorization stores.
/// </summary>
/// <remarks>
///     <para>
///         <b>Cache Levels:</b>
///         <list type="bullet">
///             <item>L1 (IMemoryCache): Fast, per-instance cache with configurable TTL</item>
///             <item>L2 (IDistributedCache via IHybridPermissionCache): Shared cache for multi-instance deployments</item>
///         </list>
///     </para>
///     <para>
///         <b>Cache Key Strategy:</b>
///         Keys include a version component that is incremented on data changes,
///         providing automatic cache invalidation without explicit removal.
///     </para>
/// </remarks>
public sealed class HybridCacheHelper
{
    private readonly IMemoryCache _l1Cache;
    private readonly IHybridPermissionCache? _hybridCache;
    private readonly ICacheMetricsService? _metrics;
    private readonly AuthorizationCacheOptions _options;

    /// <summary>
    ///     Initializes a new instance of <see cref="HybridCacheHelper"/>.
    /// </summary>
    /// <param name="l1Cache">The memory cache for L1.</param>
    /// <param name="options">Cache configuration options.</param>
    /// <param name="hybridCache">Optional hybrid cache for L2 distributed caching.</param>
    /// <param name="metrics">Optional cache metrics service.</param>
    public HybridCacheHelper(
        IMemoryCache l1Cache,
        IOptions<AuthorizationCacheOptions> options,
        IHybridPermissionCache? hybridCache = null,
        ICacheMetricsService? metrics = null)
    {
        _l1Cache = l1Cache;
        _options = options.Value;
        _hybridCache = hybridCache;
        _metrics = metrics;
    }

    /// <summary>
    ///     Attempts to get a value from the hybrid cache (L1 first, then L2).
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The cache type for metrics tracking (e.g., "policy", "acl").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the value if found, or a miss indicator.</returns>
    public async Task<CacheResult<T>> GetAsync<T>(
        string cacheKey,
        string cacheType,
        CancellationToken cancellationToken = default) where T : class
    {
        // Try L1 cache first
        if (_l1Cache.TryGetValue(cacheKey, out T? cachedValue) && cachedValue != null)
        {
            _metrics?.RecordHit(CacheLevel.L1, cacheType);
            return CacheResult<T>.Hit(cachedValue, CacheLevel.L1);
        }

        // Try L2 (hybrid) cache if available
        if (_hybridCache != null)
        {
            var hybridResult = await _hybridCache.GetAsync<T>(cacheKey, cacheType, cancellationToken).ConfigureAwait(false);
            if (hybridResult != null)
            {
                _metrics?.RecordHit(CacheLevel.L2, cacheType);
                // Promote to L1
                SetL1(cacheKey, hybridResult, GetTtlSeconds(cacheType));
                return CacheResult<T>.Hit(hybridResult, CacheLevel.L2);
            }
        }

        // Cache miss
        _metrics?.RecordMiss(cacheType);
        return CacheResult<T>.Miss();
    }

    /// <summary>
    ///     Attempts to get a value type from the hybrid cache (L1 first, then L2).
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve (must be a value type).</typeparam>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The cache type for metrics tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the value if found, or a miss indicator.</returns>
    public async Task<CacheValueResult<T>> GetValueAsync<T>(
        string cacheKey,
        string cacheType,
        CancellationToken cancellationToken = default) where T : struct
    {
        // Try L1 cache first
        if (_l1Cache.TryGetValue(cacheKey, out T cachedValue))
        {
            _metrics?.RecordHit(CacheLevel.L1, cacheType);
            return CacheValueResult<T>.Hit(cachedValue, CacheLevel.L1);
        }

        // Try L2 (hybrid) cache if available
        if (_hybridCache != null)
        {
            var hybridResult = await _hybridCache.GetValueAsync<T>(cacheKey, cacheType, cancellationToken).ConfigureAwait(false);
            if (hybridResult.Found)
            {
                _metrics?.RecordHit(CacheLevel.L2, cacheType);
                // Promote to L1
                SetL1(cacheKey, hybridResult.Value, GetTtlSeconds(cacheType));
                return CacheValueResult<T>.Hit(hybridResult.Value, CacheLevel.L2);
            }
        }

        // Cache miss
        _metrics?.RecordMiss(cacheType);
        return CacheValueResult<T>.Miss();
    }

    /// <summary>
    ///     Sets a value in both L1 and L2 caches.
    /// </summary>
    /// <typeparam name="T">The type of value to cache.</typeparam>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="cacheType">The cache type for TTL lookup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        string cacheType,
        CancellationToken cancellationToken = default)
    {
        var ttlSeconds = GetTtlSeconds(cacheType);
        
        // Set in L1
        SetL1(cacheKey, value, ttlSeconds);

        // Set in L2 if available
        if (_hybridCache != null)
        {
            await _hybridCache.SetAsync(cacheKey, value, cacheType, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Sets a value only in L1 cache (used for L2 promotion).
    /// </summary>
    /// <typeparam name="T">The type of value to cache.</typeparam>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="ttlSeconds">Time-to-live in seconds.</param>
    public void SetL1<T>(string cacheKey, T value, int ttlSeconds)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(ttlSeconds))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(ttlSeconds * 2))
            .SetSize(1);

        _l1Cache.Set(cacheKey, value, cacheOptions);
    }

    /// <summary>
    ///     Removes a value from both L1 and L2 caches.
    /// </summary>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RemoveAsync(
        string cacheKey,
        string cacheType,
        CancellationToken cancellationToken = default)
    {
        // Remove from L1
        _l1Cache.Remove(cacheKey);
        _metrics?.RecordEviction(CacheLevel.L1, cacheType);

        // Remove from L2 if available
        if (_hybridCache != null)
        {
            await _hybridCache.RemoveAsync(cacheKey, cacheType, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Removes multiple values from L1 cache.
    /// </summary>
    /// <param name="cacheKeys">The cache keys to remove.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    public void RemoveL1Many(IEnumerable<string> cacheKeys, string cacheType)
    {
        foreach (var key in cacheKeys)
        {
            _l1Cache.Remove(key);
            _metrics?.RecordEviction(CacheLevel.L1, cacheType);
        }
    }

    /// <summary>
    ///     Gets the TTL in seconds for a given cache type.
    /// </summary>
    private int GetTtlSeconds(string cacheType)
    {
        return cacheType.ToLowerInvariant() switch
        {
            "policy" => _options.PolicyTtlSeconds,
            "acl" => _options.AclTtlSeconds,
            "permission" => _options.PermissionTtlSeconds,
            _ => _options.DefaultTtlSeconds
        };
    }
}

/// <summary>
///     Result of a cache lookup for reference types.
/// </summary>
/// <typeparam name="T">The type of cached value.</typeparam>
public readonly struct CacheResult<T> where T : class
{
    /// <summary>
    ///     Whether the value was found in cache.
    /// </summary>
    public bool Found { get; }
    
    /// <summary>
    ///     The cached value (null if not found).
    /// </summary>
    public T? Value { get; }
    
    /// <summary>
    ///     The cache level where the value was found.
    /// </summary>
    public CacheLevel? Level { get; }

    private CacheResult(bool found, T? value, CacheLevel? level)
    {
        Found = found;
        Value = value;
        Level = level;
    }

    /// <summary>
    ///     Creates a cache hit result.
    /// </summary>
    public static CacheResult<T> Hit(T value, CacheLevel level) => new(true, value, level);
    
    /// <summary>
    ///     Creates a cache miss result.
    /// </summary>
    public static CacheResult<T> Miss() => new(false, null, null);
}

/// <summary>
///     Result of a cache lookup for value types.
/// </summary>
/// <typeparam name="T">The type of cached value.</typeparam>
public readonly struct CacheValueResult<T> where T : struct
{
    /// <summary>
    ///     Whether the value was found in cache.
    /// </summary>
    public bool Found { get; }
    
    /// <summary>
    ///     The cached value (default if not found).
    /// </summary>
    public T Value { get; }
    
    /// <summary>
    ///     The cache level where the value was found.
    /// </summary>
    public CacheLevel? Level { get; }

    private CacheValueResult(bool found, T value, CacheLevel? level)
    {
        Found = found;
        Value = value;
        Level = level;
    }

    /// <summary>
    ///     Creates a cache hit result.
    /// </summary>
    public static CacheValueResult<T> Hit(T value, CacheLevel level) => new(true, value, level);
    
    /// <summary>
    ///     Creates a cache miss result.
    /// </summary>
    public static CacheValueResult<T> Miss() => new(false, default, null);
}

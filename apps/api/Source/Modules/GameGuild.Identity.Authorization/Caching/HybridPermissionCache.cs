using System.Collections.Concurrent;
using System.Text.Json;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>
///     Abstraction for a hybrid (L1 + L2) permission cache.
/// </summary>
public interface IHybridPermissionCache
{
    /// <summary>
    ///     Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or default if not found.</returns>
    Task<T?> GetAsync<T>(string key, string cacheType, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    ///     Gets a value from the cache (value types).
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value wrapped in a result, or null if not found.</returns>
    Task<CacheResult<T>> GetValueAsync<T>(string key, string cacheType, CancellationToken cancellationToken = default) where T : struct;

    /// <summary>
    ///     Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, string cacheType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets a value in the cache with custom TTL.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="ttlSeconds">TTL override in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, string cacheType, int ttlSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets a value type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of value to store (must be a value type).</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetValueAsync<T>(string key, T value, string cacheType, CancellationToken cancellationToken = default) where T : struct;

    /// <summary>
    ///     Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, string cacheType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates all cache entries matching a pattern.
    /// </summary>
    /// <param name="pattern">The key pattern to match.</param>
    /// <param name="cacheType">The cache type for metrics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePatternAsync(string pattern, string cacheType, CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of a cache lookup for value types.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct CacheResult<T> where T : struct
{
    /// <summary>
    ///     Whether the value was found in the cache.
    /// </summary>
    public bool Found { get; init; }

    /// <summary>
    ///     The cached value.
    /// </summary>
    public T Value { get; init; }

    /// <summary>
    ///     Creates a found result.
    /// </summary>
    public static CacheResult<T> Hit(T value) => new() { Found = true, Value = value };

    /// <summary>
    ///     Creates a miss result.
    /// </summary>
    public static CacheResult<T> Miss() => new() { Found = false, Value = default };
}

/// <summary>
///     Hybrid cache implementation with L1 (in-memory) and optional L2 (distributed/Redis) cache.
/// </summary>
/// <remarks>
///     <para>
///         <b>Cache Levels:</b>
///         <list type="bullet">
///             <item>L1 (IMemoryCache): Fast, per-instance, short TTL</item>
///             <item>L2 (IDistributedCache): Shared across instances, longer TTL</item>
///         </list>
///     </para>
///     <para>
///         <b>Read Flow:</b> L1 → L2 → Database
///         <b>Write Flow:</b> Database → Invalidate L1 + L2
///     </para>
/// </remarks>
public sealed class HybridPermissionCache : IHybridPermissionCache
{
    private readonly IMemoryCache _l1Cache;
    private readonly IDistributedCache? _l2Cache;
    private readonly ICacheMetricsService _metrics;
    private readonly AuthorizationCacheOptions _options;
    private readonly ILogger<HybridPermissionCache> _logger;
    private readonly bool _useL2;
    private readonly ConcurrentDictionary<string, byte> _l1Keys = new(StringComparer.Ordinal);

    /// <summary>
    ///     Initializes a new instance of <see cref="HybridPermissionCache"/>.
    /// </summary>
    public HybridPermissionCache(
        IMemoryCache l1Cache,
        IOptions<AuthorizationCacheOptions> options,
        ICacheMetricsService metrics,
        ILogger<HybridPermissionCache> logger,
        IDistributedCache? l2Cache = null)
    {
        _l1Cache = l1Cache;
        _l2Cache = l2Cache;
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;
        _useL2 = _options.UseDistributedCache && _l2Cache != null;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, string cacheType, CancellationToken cancellationToken = default) where T : class
    {
        // Try L1 first
        if (_l1Cache.TryGetValue(key, out T? l1Value) && l1Value != null)
        {
            _metrics.RecordHit(CacheLevel.L1, cacheType);
            return l1Value;
        }

        // Try L2 if enabled
        if (_useL2)
        {
            try
            {
                var l2Bytes = await _l2Cache!.GetAsync(key, cancellationToken).ConfigureAwait(false);
                if (l2Bytes != null && l2Bytes.Length > 0)
                {
                    var l2Value = JsonSerializer.Deserialize<T>(l2Bytes);
                    if (l2Value != null)
                    {
                        _metrics.RecordHit(CacheLevel.L2, cacheType);

                        // Promote to L1
                        SetL1(key, l2Value);

                        return l2Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // L2 failure should not break the application
                _logger.LogWarning(ex, "L2 cache read failed for key {Key}, falling back to database", key);
                throw;
            }
        }

        _metrics.RecordMiss(cacheType);
        return null;
    }

    /// <inheritdoc />
    public async Task<CacheResult<T>> GetValueAsync<T>(string key, string cacheType, CancellationToken cancellationToken = default) where T : struct
    {
        // Try L1 first
        if (_l1Cache.TryGetValue(key, out T l1Value))
        {
            _metrics.RecordHit(CacheLevel.L1, cacheType);
            return CacheResult<T>.Hit(l1Value);
        }

        // Try L2 if enabled
        if (_useL2)
        {
            try
            {
                var l2Bytes = await _l2Cache!.GetAsync(key, cancellationToken).ConfigureAwait(false);
                if (l2Bytes != null && l2Bytes.Length > 0)
                {
                    var l2Value = JsonSerializer.Deserialize<T>(l2Bytes);
                    _metrics.RecordHit(CacheLevel.L2, cacheType);

                    // Promote to L1
                    SetL1(key, l2Value);

                    return CacheResult<T>.Hit(l2Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache read failed for key {Key}", key);
                throw;
            }
        }

        _metrics.RecordMiss(cacheType);
        return CacheResult<T>.Miss();
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, string cacheType, CancellationToken cancellationToken = default)
    {
        return SetAsyncCore(key, value, cacheType, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, string cacheType, int ttlSeconds, CancellationToken cancellationToken = default)
    {
        return SetAsyncCore(key, value, cacheType, ttlSeconds, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetValueAsync<T>(string key, T value, string cacheType, CancellationToken cancellationToken = default) where T : struct
    {
        return SetAsyncCore(key, value, cacheType, null, cancellationToken);
    }

    private async Task SetAsyncCore<T>(string key, T value, string cacheType, int? ttlSeconds, CancellationToken cancellationToken)
    {
        var l1Ttl = TimeSpan.FromSeconds(ttlSeconds ?? _options.PermissionTtlSeconds);
        var l2Ttl = TimeSpan.FromSeconds(_options.DistributedCacheTtlSeconds);

        // Set in L1
        SetL1(key, value, l1Ttl);

        // Set in L2 if enabled
        if (_useL2)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = l2Ttl
                };
                await _l2Cache!.SetAsync(key, bytes, distributedOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache write failed for key {Key}", key);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, string cacheType, CancellationToken cancellationToken = default)
    {
        // Remove from L1
        _l1Cache.Remove(key);
        _metrics.RecordEviction(CacheLevel.L1, cacheType, "explicit");

        // Remove from L2 if enabled
        if (_useL2)
        {
            try
            {
                await _l2Cache!.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                _metrics.RecordEviction(CacheLevel.L2, cacheType, "explicit");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache remove failed for key {Key}", key);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task InvalidatePatternAsync(string pattern, string cacheType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matchingKeys = _l1Keys.Keys
            .Where(key => MatchesPattern(key, pattern))
            .ToArray();

        foreach (var key in matchingKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _l1Cache.Remove(key);
            _l1Keys.TryRemove(key, out _);
            _metrics.RecordEviction(CacheLevel.L1, cacheType, "pattern");

            if (!_useL2)
            {
                continue;
            }

            try
            {
                await _l2Cache!.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                _metrics.RecordEviction(CacheLevel.L2, cacheType, "pattern");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache pattern remove failed for key {Key}", key);
                throw;
            }
        }

        _logger.LogDebug(
            "Pattern invalidation removed {Count} tracked cache keys for pattern {Pattern}",
            matchingKeys.Length,
            pattern);
    }

    private void SetL1<T>(string key, T value, TimeSpan? ttl = null)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ttl ?? TimeSpan.FromSeconds(_options.PermissionTtlSeconds))
            .SetSlidingExpiration(TimeSpan.FromSeconds(_options.PermissionTtlSeconds / 2))
            .SetSize(1)
            .RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                if (evictedKey is string cacheKey)
                {
                    _l1Keys.TryRemove(cacheKey, out _);
                }
            });

        _l1Keys[key] = 0;
        _l1Cache.Set(key, value, cacheOptions);
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        if (pattern == "*")
        {
            return true;
        }

        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            return string.Equals(key, pattern, StringComparison.Ordinal);
        }

        var segments = pattern.Split('*');
        var currentIndex = 0;

        if (segments[0].Length > 0)
        {
            if (!key.StartsWith(segments[0], StringComparison.Ordinal))
            {
                return false;
            }

            currentIndex = segments[0].Length;
        }

        foreach (var segment in segments.Skip(1))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            var segmentIndex = key.IndexOf(segment, currentIndex, StringComparison.Ordinal);
            if (segmentIndex < 0)
            {
                return false;
            }

            currentIndex = segmentIndex + segment.Length;
        }

        var finalSegment = segments[^1];
        return finalSegment.Length == 0 || key.EndsWith(finalSegment, StringComparison.Ordinal);
    }
}

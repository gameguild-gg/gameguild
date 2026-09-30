using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>Tracks authorization cache keys so invalidation can remove entries from process-wide L1.</summary>
public interface IPermissionCacheKeyTracker
{
    /// <summary>Gets a process-local snapshot of the tracked L1 cache entries.</summary>
    PermissionCacheKeyTrackerStatistics GetStatistics();

    /// <summary>Tracks a key and removes its registration when the L1 entry expires or is evicted.</summary>
    void Track(string key, string cacheType);

    /// <summary>Tracks a key and removes its registration when the L1 entry expires or is evicted.</summary>
    void Track(string key, string cacheType, MemoryCacheEntryOptions? entryOptions);

    /// <summary>Removes a key from the index without changing the L1 cache.</summary>
    void Forget(string key);

    /// <summary>Removes all matching keys from L1 and the index.</summary>
    int Invalidate(Func<string, bool> predicate, string fallbackCacheType, string reason);

    /// <summary>Removes keys matching a glob pattern. An asterisk matches any sequence of characters.</summary>
    int InvalidatePattern(string pattern, string cacheType, string reason);
}

/// <summary>Process-wide index of entries stored in the shared authorization L1 cache.</summary>
public sealed class PermissionCacheKeyTracker : IPermissionCacheKeyTracker
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheMetricsService _metrics;
    private readonly int _maxTrackedEntries;
    private readonly ConcurrentDictionary<string, CacheKeyRegistration> _keys = new(StringComparer.Ordinal);
    private long _nextRegistrationId;

    public PermissionCacheKeyTracker(
        IMemoryCache memoryCache,
        ICacheMetricsService metrics)
        : this(memoryCache, metrics, 5000)
    {
    }

    public PermissionCacheKeyTracker(
        IMemoryCache memoryCache,
        ICacheMetricsService metrics,
        int maxTrackedEntries)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(metrics);
        if (maxTrackedEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTrackedEntries), "The cache entry limit must be positive.");
        }

        _memoryCache = memoryCache;
        _metrics = metrics;
        _maxTrackedEntries = maxTrackedEntries;
    }

    /// <inheritdoc />
    public PermissionCacheKeyTrackerStatistics GetStatistics()
    {
        var entries = _keys.ToArray();
        var users = entries
            .Select(entry => TryGetUserId(entry.Key))
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .Count();

        return new PermissionCacheKeyTrackerStatistics(
            entries.LongLength,
            users,
            CountEntries(entries, "permission"),
            CountEntries(entries, "acl"),
            CountEntries(entries, "policy"));
    }

    /// <inheritdoc />
    public void Track(string key, string cacheType)
    {
        Track(key, cacheType, null);
    }

    /// <inheritdoc />
    public void Track(string key, string cacheType, MemoryCacheEntryOptions? entryOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheType);

        var registration = new CacheKeyRegistration(cacheType, Interlocked.Increment(ref _nextRegistrationId));
        _keys[key] = registration;

        entryOptions?.RegisterPostEvictionCallback(
            static (evictedKey, _, _, state) =>
            {
                if (evictedKey is string key && state is EvictionState evictionState)
                {
                    evictionState.Tracker.Forget(key, evictionState.RegistrationId);
                }
            },
            new EvictionState(this, registration.RegistrationId));

        EvictOverCapacity(registration.RegistrationId);
    }

    /// <inheritdoc />
    public void Forget(string key) => _keys.TryRemove(key, out _);

    /// <inheritdoc />
    public int Invalidate(Func<string, bool> predicate, string fallbackCacheType, string reason)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var removed = 0;
        foreach (var entry in _keys)
        {
            if (!predicate(entry.Key) || !RemoveRegistration(entry.Key, entry.Value))
            {
                continue;
            }

            _memoryCache.Remove(entry.Key);
            var cacheType = entry.Value.CacheType == "all" ? fallbackCacheType : entry.Value.CacheType;
            _metrics.RecordEviction(CacheLevel.L1, cacheType, reason);
            removed++;
        }

        return removed;
    }

    /// <inheritdoc />
    public int InvalidatePattern(string pattern, string cacheType, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        return Invalidate(key => MatchesPattern(key, pattern), cacheType, reason);
    }

    private void Forget(string key, long registrationId)
    {
        if (_keys.TryGetValue(key, out var current) && current.RegistrationId == registrationId)
        {
            RemoveRegistration(key, current);
        }
    }

    private void EvictOverCapacity(long protectedRegistrationId)
    {
        while (_keys.Count > _maxTrackedEntries)
        {
            var oldestAvailable = _keys
                .Where(entry => entry.Value.RegistrationId != protectedRegistrationId)
                .MinBy(entry => entry.Value.RegistrationId);
            if (oldestAvailable.Key is null)
            {
                return;
            }

            if (!RemoveRegistration(oldestAvailable.Key, oldestAvailable.Value))
            {
                continue;
            }

            _memoryCache.Remove(oldestAvailable.Key);
            var cacheType = oldestAvailable.Value.CacheType == "all" ? "permission" : oldestAvailable.Value.CacheType;
            _metrics.RecordEviction(CacheLevel.L1, cacheType, "capacity");
        }
    }

    private bool RemoveRegistration(string key, CacheKeyRegistration registration)
    {
        return ((ICollection<KeyValuePair<string, CacheKeyRegistration>>)_keys)
            .Remove(new KeyValuePair<string, CacheKeyRegistration>(key, registration));
    }

    private static long CountEntries(KeyValuePair<string, CacheKeyRegistration>[] entries, string cacheType) =>
        entries.LongCount(entry => string.Equals(entry.Value.CacheType, cacheType, StringComparison.OrdinalIgnoreCase));

    private static Guid? TryGetUserId(string key)
    {
        var segments = key.Split(new[] { ':', '|' }, StringSplitOptions.RemoveEmptyEntries);
        var userSegment = segments.Length >= 4 &&
                          string.Equals(segments[0], "acl", StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(segments[1], "subj", StringComparison.OrdinalIgnoreCase)
            ? 3
            : segments.Length >= 3 &&
              (string.Equals(segments[0], "acl", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(segments[0], "perm", StringComparison.OrdinalIgnoreCase))
            ? 2
                : -1;

        return userSegment >= 0 && Guid.TryParse(segments[userSegment], out var userId) ? userId : null;
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        var searchFrom = 0;
        foreach (var segment in pattern.Split('*'))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            var matchAt = value.IndexOf(segment, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (matchAt < 0)
            {
                return false;
            }

            searchFrom = matchAt + segment.Length;
        }

        return true;
    }

    private sealed record CacheKeyRegistration(string CacheType, long RegistrationId);

    private sealed record EvictionState(PermissionCacheKeyTracker Tracker, long RegistrationId);
}

/// <summary>Process-local counts for currently tracked authorization L1 cache entries.</summary>
public sealed record PermissionCacheKeyTrackerStatistics(
    long TotalEntries,
    int UniqueUsers,
    long PermissionEntries,
    long AclEntries,
    long PolicyEntries);

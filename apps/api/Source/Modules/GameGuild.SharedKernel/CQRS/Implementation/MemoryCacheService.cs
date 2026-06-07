using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     In-memory cache service implementation using <see cref="IMemoryCache"/>.
/// </summary>
/// <remarks>
///     All methods return completed tasks because <see cref="IMemoryCache"/> is inherently synchronous.
///     The async signatures conform to the <see cref="ICacheService"/> interface contract,
///     enabling drop-in replacement with distributed cache implementations (Redis, SQL Server)
///     without changing the calling code.
/// </remarks>
public sealed class MemoryCacheService : IPatternCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    /// <summary>
    ///     Initializes a new instance of the MemoryCacheService class
    /// </summary>
    /// <param name="memoryCache">Memory cache</param>
    public MemoryCacheService(IMemoryCache memoryCache) { _memoryCache = memoryCache; }

    /// <summary>
    ///     Gets a value from cache
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached value or null</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = _memoryCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue ? typedValue : default;

        return Task.FromResult(value);
    }

    /// <summary>
    ///     Sets a value in cache
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _memoryCache.Set(key, value, expiration);
        _keys[key] = 0;

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _memoryCache.Remove(key);
        _keys.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    public Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        var matchingKeys = _keys.Keys.Where(key => MatchesPattern(key, pattern)).ToArray();

        foreach (var key in matchingKeys)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.FromResult(matchingKeys.Length);
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        if (pattern == "*") { return true; }

        if (!pattern.Contains('*') && !pattern.Contains('?')) { return string.Equals(key, pattern, StringComparison.Ordinal); }

        var keyIndex = 0;
        var patternIndex = 0;
        var starIndex = -1;
        var matchIndex = 0;

        while (keyIndex < key.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == key[keyIndex]))
            {
                patternIndex++;
                keyIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex++;
                matchIndex = keyIndex;
            }
            else if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                keyIndex = ++matchIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*') { patternIndex++; }

        return patternIndex == pattern.Length;
    }
}

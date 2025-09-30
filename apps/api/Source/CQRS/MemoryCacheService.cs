using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.CQRS;

/// <summary> In-memory cache service implementation </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    /// <summary> Initializes a new instance of the MemoryCacheService class </summary>
    /// <param name="memoryCache"> Memory cache </param>
    public MemoryCacheService(IMemoryCache memoryCache) { _memoryCache = memoryCache; }

    /// <summary> Gets a value from cache </summary>
    /// <typeparam name="T"> Value type </typeparam>
    /// <param name="key"> Cache key </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Cached value or null </returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = _memoryCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue ? typedValue : default(T?);

        return Task.FromResult(value);
    }

    /// <summary> Sets a value in cache </summary>
    /// <typeparam name="T"> Value type </typeparam>
    /// <param name="key"> Cache key </param>
    /// <param name="value"> Value to cache </param>
    /// <param name="expiration"> Expiration time </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Task </returns>
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _memoryCache.Set(key, value, expiration);

        return Task.CompletedTask;
    }

    /// <summary> Gets a value from cache or sets it using the factory if not found </summary>
    /// <typeparam name="T"> Value type </typeparam>
    /// <param name="key"> Cache key </param>
    /// <param name="factory"> Factory function to create the value if not cached </param>
    /// <param name="expiration"> Cache expiration time </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Cached or newly created value </returns>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_memoryCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
        {
            return typedValue;
        }

        var value = await factory().ConfigureAwait(false);

        _memoryCache.Set(key, value, expiration);

        return value;
    }

    /// <summary> Removes a value from cache </summary>
    /// <param name="key"> Cache key </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Task </returns>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _memoryCache.Remove(key);

        return Task.CompletedTask;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Distributed cache implementation backed by <see cref="IDistributedCache"/>.
/// </summary>
public sealed class DistributedCacheService : IPatternCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var json = await _cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _cache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
            cancellationToken).ConfigureAwait(false);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            throw new NotSupportedException("The configured distributed cache does not expose key enumeration. Configure provider-specific Redis SCAN support for wildcard cache clearing.");
        }

        await RemoveAsync(pattern, cancellationToken).ConfigureAwait(false);
        return 1;
    }
}

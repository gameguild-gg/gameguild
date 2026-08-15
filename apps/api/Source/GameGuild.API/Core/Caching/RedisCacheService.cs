using System.Text.Json;
using GameGuild.Configuration.InfrastructureLayer.RedisCaching;
using GameGuild.CQRS;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace GameGuild.API;

internal sealed class RedisCacheService(
    IDistributedCache cache,
    IConnectionMultiplexer redis,
    RedisCachingOptions options) : IPatternCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var json = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var json = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
            cancellationToken).ConfigureAwait(false);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        if (!ContainsWildcard(pattern))
        {
            await RemoveAsync(pattern, cancellationToken).ConfigureAwait(false);
            return 1;
        }

        var database = redis.GetDatabase();
        var redisPattern = BuildRedisPattern(pattern);
        var keys = new HashSet<RedisKey>();

        foreach (var endpoint in redis.GetEndPoints())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var server = redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            foreach (var key in server.Keys(database.Database, redisPattern))
            {
                cancellationToken.ThrowIfCancellationRequested();
                keys.Add(key);
            }
        }

        if (keys.Count == 0)
        {
            return 0;
        }

        var removed = await database.KeyDeleteAsync(keys.ToArray()).ConfigureAwait(false);
        return checked((int)removed);
    }

    private RedisValue BuildRedisPattern(string pattern)
    {
        var instanceName = options.InstanceName;
        if (string.IsNullOrWhiteSpace(instanceName) || pattern.StartsWith(instanceName, StringComparison.Ordinal))
        {
            return pattern;
        }

        return $"{instanceName}{pattern}";
    }

    private static bool ContainsWildcard(string pattern)
        => pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal);
}

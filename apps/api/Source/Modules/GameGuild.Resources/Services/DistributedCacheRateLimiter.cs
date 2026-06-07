using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Portable distributed-cache rate limiter used when a raw Redis connection is not configured.
/// </summary>
public sealed class DistributedCacheRateLimiter(
    IDistributedCache cache,
    ILogger<DistributedCacheRateLimiter> logger) : IDistributedRateLimiter
{
    private const string KeyPrefix = "ratelimit:";

    public async Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetWindowKey(key, window);
        var count = await GetCountAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (count >= maxRequests)
        {
            logger.LogWarning("Rate limit exceeded for key {Key}: {CurrentCount}/{MaxRequests} in {Window}", key, count, maxRequests, window);
            return false;
        }

        await SetCountAsync(cacheKey, count + 1, window, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<int> GetCurrentCountAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        return await GetCountAsync(GetWindowKey(key, window), cancellationToken).ConfigureAwait(false);
    }

    public Task<TimeSpan?> GetTimeUntilResetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)window.TotalMilliseconds;
        var windowStart = now / windowMs * windowMs;
        var resetAt = windowStart + windowMs;
        return Task.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(Math.Max(0, resetAt - now)));
    }

    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var wildcardKey = $"{KeyPrefix}{key}:";
        logger.LogInformation("Distributed cache limiter reset requested for {KeyPrefix}; specific rolling-window keys expire automatically.", wildcardKey);
        return Task.CompletedTask;
    }

    private async Task<int> GetCountAsync(string key, CancellationToken cancellationToken)
    {
        var value = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        return int.TryParse(value, out var count) ? count : 0;
    }

    private Task SetCountAsync(string key, int count, TimeSpan window, CancellationToken cancellationToken)
    {
        return cache.SetStringAsync(
            key,
            count.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window.Add(TimeSpan.FromMinutes(1)) },
            cancellationToken);
    }

    private static string GetWindowKey(string key, TimeSpan window)
    {
        var windowMs = (long)window.TotalMilliseconds;
        var windowStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / windowMs * windowMs;
        return $"{KeyPrefix}{key}:{windowStart}";
    }
}

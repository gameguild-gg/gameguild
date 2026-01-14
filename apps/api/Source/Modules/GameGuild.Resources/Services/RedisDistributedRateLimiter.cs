using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GameGuild.Resources;

/// <summary>
///     Redis-backed distributed rate limiter using sliding window algorithm
/// </summary>
public class RedisDistributedRateLimiter : IDistributedRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedRateLimiter> _logger;
    private const string KeyPrefix = "ratelimit:";

    public RedisDistributedRateLimiter(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = GetRedisKey(key);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)window.TotalMilliseconds;

        try
        {
            // Sliding window algorithm using sorted set
            // 1. Remove expired entries
            await db.SortedSetRemoveRangeByScoreAsync(redisKey, 0, windowStart);

            // 2. Count current requests in window
            var currentCount = await db.SortedSetLengthAsync(redisKey);

            // 3. Check if under limit
            if (currentCount >= maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for key {Key}: {CurrentCount}/{MaxRequests} in {Window}",
                    key, currentCount, maxRequests, window);
                return false;
            }

            // 4. Add current request with timestamp as score
            var requestId = Guid.NewGuid().ToString("N");
            await db.SortedSetAddAsync(redisKey, requestId, now);

            // 5. Set expiry on key (cleanup)
            await db.KeyExpireAsync(redisKey, window.Add(TimeSpan.FromMinutes(1)));

            _logger.LogDebug("Rate limit check passed for key {Key}: {CurrentCount}/{MaxRequests}",
                key, currentCount + 1, maxRequests);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis rate limiter error for key {Key}. Allowing request (fail-open for availability).", key);
            // Fail open - allow request if Redis is unavailable
            return true;
        }
    }

    public async Task<int> GetCurrentCountAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = GetRedisKey(key);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)window.TotalMilliseconds();

        try
        {
            // Remove expired entries first
            await db.SortedSetRemoveRangeByScoreAsync(redisKey, 0, windowStart);

            // Count current requests
            var count = await db.SortedSetLengthAsync(redisKey);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current count for key {Key}", key);
            return 0;
        }
    }

    public async Task<TimeSpan?> GetTimeUntilResetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = GetRedisKey(key);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)window.TotalMilliseconds;

        try
        {
            // Get oldest entry in current window
            var oldestEntries = await db.SortedSetRangeByScoreWithScoresAsync(
                redisKey,
                start: windowStart,
                stop: double.PositiveInfinity,
                take: 1);

            if (oldestEntries.Length == 0)
                return null;

            var oldestTimestamp = (long)oldestEntries[0].Score;
            var resetTime = oldestTimestamp + (long)window.TotalMilliseconds;
            var timeUntilReset = resetTime - now;

            return timeUntilReset > 0 ? TimeSpan.FromMilliseconds(timeUntilReset) : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reset time for key {Key}", key);
            return null;
        }
    }

    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = GetRedisKey(key);

        try
        {
            await db.KeyDeleteAsync(redisKey);
            _logger.LogInformation("Rate limit reset for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for key {Key}", key);
            throw;
        }
    }

    private static string GetRedisKey(string key) => $"{KeyPrefix}{key}";
}

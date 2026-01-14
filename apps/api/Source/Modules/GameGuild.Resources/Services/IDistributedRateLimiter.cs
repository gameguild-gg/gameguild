namespace GameGuild.Resources;

/// <summary>
///     Distributed rate limiter using Redis for horizontal scaling
/// </summary>
public interface IDistributedRateLimiter
{
    /// <summary>
    ///     Check if request is allowed under rate limit using sliding window algorithm
    /// </summary>
    /// <param name="key">Rate limit key (e.g., "user:123:api-calls")</param>
    /// <param name="maxRequests">Maximum requests allowed in window</param>
    /// <param name="window">Time window for rate limit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if request is allowed, false if rate limit exceeded</returns>
    Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get current request count for a key
    /// </summary>
    Task<int> GetCurrentCountAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get time until rate limit resets
    /// </summary>
    Task<TimeSpan?> GetTimeUntilResetAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reset rate limit for a key (admin operation)
    /// </summary>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
}

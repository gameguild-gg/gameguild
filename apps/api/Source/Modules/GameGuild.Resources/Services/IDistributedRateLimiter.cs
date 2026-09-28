namespace GameGuild.Resources;

public readonly record struct RateLimitDecision(bool IsAllowed, TimeSpan RetryAfter);

public readonly record struct ConcurrencyLimitDecision(
    bool IsAllowed,
    TimeSpan RetryAfter,
    IAsyncDisposable? Lease = null);

/// <summary>
///     Distributed rate limiter using Redis for horizontal scaling
/// </summary>
public interface IDistributedRateLimiter
{
    /// <summary>
    ///     Atomically checks and consumes a permit in the current wall-clock fixed window.
    /// </summary>
    Task<bool> IsAllowedFixedWindowAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consumes one token from a bucket and returns the wait time when empty.
    /// </summary>
    Task<RateLimitDecision> TryAcquireTokenBucketAsync(
        string key,
        int tokenLimit,
        int tokensPerPeriod,
        TimeSpan replenishmentPeriod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically acquires a concurrency permit with an expiring, renewable lease.
    /// The caller must dispose the returned lease when the protected operation completes.
    /// </summary>
    Task<ConcurrencyLimitDecision> TryAcquireConcurrencyLeaseAsync(
        string key,
        int maxConcurrent,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the remaining duration of an active temporary penalty, if any.
    /// </summary>
    Task<TimeSpan?> GetActivePenaltyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a violation and applies an exponentially increasing temporary penalty
    /// once the configured threshold is reached.
    /// </summary>
    Task<TimeSpan?> RecordRateLimitViolationAsync(
        string key,
        int violationThreshold,
        TimeSpan decayWindow,
        TimeSpan basePenalty,
        TimeSpan maxPenalty,
        CancellationToken cancellationToken = default);

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

namespace GameGuild.Configuration.PresentationLayer.Authorization;

/// <summary>
///     Configuration options for authorization caching.
/// </summary>
public sealed class AuthorizationCacheOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name.
    /// </summary>
    public const string SectionName = "Authorization:Cache";

    // ========================
    // TTL CONFIGURATION
    // ========================

    /// <summary>
    ///     Time-to-live in seconds for cached policy definitions.
    /// </summary>
    public int PolicyTtlSeconds { get; set; } = 300;

    /// <summary>
    ///     Time-to-live in seconds for cached permission lookups.
    /// </summary>
    public int PermissionTtlSeconds { get; set; } = 300;

    /// <summary>
    ///     Time-to-live in seconds for cached Access Control List lookups.
    /// </summary>
    public int AccessControlListTtlSeconds { get; set; } = 60;

    /// <summary>
    ///     Time-to-live in seconds for cached rulesets.
    /// </summary>
    public int RulesetTtlSeconds { get; set; } = 300;

    // ========================
    // CACHE SIZE LIMITS
    // ========================

    /// <summary>
    ///     Maximum number of cached policy entries.
    /// </summary>
    public int MaxPolicyCacheSize { get; set; } = 1000;

    /// <summary>
    ///     Maximum number of cached permission entries in L1 (memory) cache.
    /// </summary>
    public int MaxL1CacheSize { get; set; } = 5000;

    // ========================
    // DISTRIBUTED CACHE (REDIS)
    // ========================

    /// <summary>
    ///     Whether to enable distributed caching (Redis) vs in-memory only.
    ///     When false, only L1 (in-memory) cache is used.
    ///     When true, L1 + L2 (distributed) cache is used.
    /// </summary>
    public bool UseDistributedCache { get; set; } = false;

    /// <summary>
    ///     Redis connection string. Only used when <see cref="UseDistributedCache"/> is true.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    ///     Redis instance name prefix for cache keys.
    /// </summary>
    public string RedisInstanceName { get; set; } = "gg:auth:";

    /// <summary>
    ///     Time-to-live in seconds for L2 (distributed) cache entries.
    ///     Should cover the longest L1 cache TTL to reduce Redis calls.
    /// </summary>
    public int DistributedCacheTtlSeconds { get; set; } = 600;

    // ========================
    // AUTOMATIC CACHE WARMING
    // ========================

    /// <summary>
    ///     Whether to warm frequently accessed subject-based ACL decisions on a schedule.
    ///     Tracking is process-local and bounded; warmed decisions still use the normal versioned L1/L2 cache path.
    /// </summary>
    public bool AutomaticWarmupEnabled { get; set; } = true;

    /// <summary>The number of seconds between automatic warmup cycles.</summary>
    public int AutomaticWarmupIntervalSeconds { get; set; } = 60;

    /// <summary>The number of accesses within one cycle required before an ACL pair is considered popular.</summary>
    public int AutomaticWarmupMinimumAccessCount { get; set; } = 5;

    /// <summary>The maximum number of popular ACL pairs warmed during one cycle.</summary>
    public int AutomaticWarmupMaxEntriesPerCycle { get; set; } = 50;

    /// <summary>The maximum number of distinct ACL pairs retained in the current popularity window.</summary>
    public int PopularityTrackingCapacity { get; set; } = 5000;

    // ========================
    // METRICS & OBSERVABILITY
    // ========================

    /// <summary>
    ///     Whether to enable cache metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    ///     Interval in seconds for logging cache statistics.
    /// </summary>
    public int MetricsLoggingIntervalSeconds { get; set; } = 60;

    /// <summary>Minimum cache hit rate that should be maintained after enough samples are available.</summary>
    public double MinimumHitRateWarningThreshold { get; set; } = 0.70;

    /// <summary>Minimum request sample before emitting cache hit-rate warnings.</summary>
    public int MinimumRequestsForPerformanceWarning { get; set; } = 100;

    /// <summary>Average lookup latency in milliseconds above which a cache type should warn.</summary>
    public double LookupLatencyWarningThresholdMilliseconds { get; set; } = 100;

    // ========================
    // CACHE COHERENCE
    // ========================

    /// <summary>
    ///     Whether to use pub/sub for cache invalidation across instances.
    ///     Requires <see cref="UseDistributedCache"/> to be true.
    /// </summary>
    public bool UsePubSubInvalidation { get; set; } = true;

    /// <summary>
    ///     Redis channel name for cache invalidation messages.
    /// </summary>
    public string InvalidationChannelName { get; set; } = "gg:auth:invalidate";

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        
        if (PolicyTtlSeconds < 0)
            throw new InvalidOperationException("PolicyTtlSeconds cannot be negative.");
        
        if (PermissionTtlSeconds < 0)
            throw new InvalidOperationException("PermissionTtlSeconds cannot be negative.");
        
        if (AccessControlListTtlSeconds < 0)
            throw new InvalidOperationException("AccessControlListTtlSeconds cannot be negative.");

        if (RulesetTtlSeconds < 0)
        {
            throw new InvalidOperationException("RulesetTtlSeconds cannot be negative.");
        }
        
        if (MaxPolicyCacheSize <= 0)
            throw new InvalidOperationException("MaxPolicyCacheSize must be positive.");
        
        if (MaxL1CacheSize <= 0)
            throw new InvalidOperationException("MaxL1CacheSize must be positive.");

        if (AutomaticWarmupIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("AutomaticWarmupIntervalSeconds must be positive.");
        }

        if (AutomaticWarmupMinimumAccessCount <= 0)
        {
            throw new InvalidOperationException("AutomaticWarmupMinimumAccessCount must be positive.");
        }

        if (AutomaticWarmupMaxEntriesPerCycle <= 0 || AutomaticWarmupMaxEntriesPerCycle > 500)
        {
            throw new InvalidOperationException("AutomaticWarmupMaxEntriesPerCycle must be between 1 and 500.");
        }

        if (PopularityTrackingCapacity <= 0)
        {
            throw new InvalidOperationException("PopularityTrackingCapacity must be positive.");
        }

        if (MetricsLoggingIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("MetricsLoggingIntervalSeconds must be positive.");
        }

        if (MinimumHitRateWarningThreshold is < 0 or > 1)
        {
            throw new InvalidOperationException("MinimumHitRateWarningThreshold must be between 0 and 1.");
        }

        if (MinimumRequestsForPerformanceWarning <= 0)
        {
            throw new InvalidOperationException("MinimumRequestsForPerformanceWarning must be positive.");
        }

        if (LookupLatencyWarningThresholdMilliseconds <= 0)
        {
            throw new InvalidOperationException("LookupLatencyWarningThresholdMilliseconds must be positive.");
        }
        
        if (UseDistributedCache && string.IsNullOrWhiteSpace(RedisConnectionString))
            throw new InvalidOperationException("RedisConnectionString is required when UseDistributedCache is true.");
        
        var longestL1TtlSeconds = Math.Max(
            Math.Max(PolicyTtlSeconds, PermissionTtlSeconds),
            Math.Max(AccessControlListTtlSeconds, RulesetTtlSeconds));
        if (DistributedCacheTtlSeconds < longestL1TtlSeconds)
        {
            throw new InvalidOperationException("DistributedCacheTtlSeconds should be >= the longest L1 cache TTL for optimal cache efficiency.");
        }
    }

    /// <summary>
    ///     Creates a default instance of AuthorizationCacheOptions.
    /// </summary>
    public static AuthorizationCacheOptions CreateDefault() => new();
}

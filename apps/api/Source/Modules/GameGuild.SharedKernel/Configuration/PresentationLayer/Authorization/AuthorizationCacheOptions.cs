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
    ///     Should be longer than L1 TTL to reduce Redis calls.
    /// </summary>
    public int DistributedCacheTtlSeconds { get; set; } = 600;

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
        
        if (MaxPolicyCacheSize <= 0)
            throw new InvalidOperationException("MaxPolicyCacheSize must be positive.");
        
        if (MaxL1CacheSize <= 0)
            throw new InvalidOperationException("MaxL1CacheSize must be positive.");
        
        if (UseDistributedCache && string.IsNullOrWhiteSpace(RedisConnectionString))
            throw new InvalidOperationException("RedisConnectionString is required when UseDistributedCache is true.");
        
        if (DistributedCacheTtlSeconds < PolicyTtlSeconds)
            throw new InvalidOperationException("DistributedCacheTtlSeconds should be >= PolicyTtlSeconds for optimal cache efficiency.");
    }

    /// <summary>
    ///     Creates a default instance of AuthorizationCacheOptions.
    /// </summary>
    public static AuthorizationCacheOptions CreateDefault() => new();
}

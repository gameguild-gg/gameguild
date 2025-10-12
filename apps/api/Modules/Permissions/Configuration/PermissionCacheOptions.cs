namespace GameGuild.Modules.Permissions.Configuration;

/// <summary>
/// Configuration options for permission caching
/// </summary>
public class PermissionCacheOptions
{
    public const string SectionName = "PermissionCache";

    /// <summary>
    /// Default cache duration for permission checks
    /// </summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Short cache duration for frequently changing permissions
    /// </summary>
    public TimeSpan ShortCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Long cache duration for stable permissions
    /// </summary>
    public TimeSpan LongCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum number of cached entries
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Cache key prefix to avoid collisions
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "perm:";

    /// <summary>
    /// Enable cache statistics collection
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Cache sliding expiration factor (multiplier for absolute expiration)
    /// </summary>
    public double SlidingExpirationFactor { get; set; } = 0.5;
}
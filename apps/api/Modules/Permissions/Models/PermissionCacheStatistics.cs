namespace GameGuild.Modules.Permissions.Models;

/// <summary>
/// Statistics for permission cache performance
/// </summary>
public class PermissionCacheStatistics
{
    /// <summary>
    /// Total cache hits
    /// </summary>
    public long TotalHits { get; set; }

    /// <summary>
    /// Total cache misses
    /// </summary>
    public long TotalMisses { get; set; }

    /// <summary>
    /// Cache hit ratio (hits / (hits + misses))
    /// </summary>
    public double HitRatio => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) : 0;

    /// <summary>
    /// Number of cached entries
    /// </summary>
    public long CachedEntries { get; set; }

    /// <summary>
    /// Cache memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Last cache invalidation time
    /// </summary>
    public DateTime? LastInvalidation { get; set; }
}
using GameGuild.Identity.Authorization.Caching;

namespace GameGuild.Identity.Authentication;

/// <summary>Process-local authorization cache statistics.</summary>
public sealed class PermissionCacheStatsDto
{
    /// <summary>Distinct user IDs represented by tracked L1 permission or ACL cache keys.</summary>
    public int TotalCachedUsers { get; set; }

    /// <summary>Tracked L1 permission cache entries.</summary>
    public int TotalCachedPermissions { get; set; }

    /// <summary>Tracked L1 ACL decision entries.</summary>
    public long TotalCachedAclEntries { get; set; }

    /// <summary>Tracked L1 policy entries.</summary>
    public long TotalCachedPolicies { get; set; }

    /// <summary>Overall cache hit rate (0-1).</summary>
    public double CacheHitRate { get; set; }

    /// <summary>Total currently tracked process-local L1 entries.</summary>
    public long CacheSize { get; set; }

    /// <summary>UTC time at which this snapshot was captured.</summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>Latency samples; empty until cache operation duration is instrumented.</summary>
    public List<CachePerformanceMetric> PerformanceMetrics { get; set; } = new List<CachePerformanceMetric>();

    public long L1Hits { get; set; }

    public long L2Hits { get; set; }

    public long CacheMisses { get; set; }

    public long CacheEvictions { get; set; }

    public long TotalRequests { get; set; }

    public double L1HitRate { get; set; }

    public double L2HitRate { get; set; }

    public Dictionary<string, CacheTypeStatistics> ByType { get; set; } = new(StringComparer.Ordinal);
}

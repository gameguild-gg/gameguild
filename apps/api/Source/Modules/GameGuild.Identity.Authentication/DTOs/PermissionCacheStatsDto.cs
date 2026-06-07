namespace GameGuild.Identity.Authentication;

public abstract class PermissionCacheStatsDto
{
    public int TotalCachedUsers { get; set; }

    public int TotalCachedPermissions { get; set; }

    public double CacheHitRate { get; set; }

    public long CacheSize { get; set; }

    public DateTime LastUpdated { get; set; }

    public List<CachePerformanceMetric> PerformanceMetrics { get; set; } = new List<CachePerformanceMetric>();
}

using GameGuild.CQRS;
using GameGuild.Identity.Authorization.Caching;

namespace GameGuild.Identity.Authentication;

/// <summary>Returns process-local cache counters and tracked L1 entry counts.</summary>
public sealed class GetPermissionCacheStatsQueryHandler(
    ICacheMetricsService metrics,
    IPermissionCacheKeyTracker keyTracker)
    : IQueryHandler<GetPermissionCacheStatsQuery, PermissionCacheStatsDto>
{
    public Task<PermissionCacheStatsDto> Handle(
        GetPermissionCacheStatsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var cache = metrics.GetStatistics();
        var keys = keyTracker.GetStatistics();

        return Task.FromResult(new PermissionCacheStatsDto
        {
            TotalCachedUsers = keys.UniqueUsers,
            TotalCachedPermissions = checked((int)keys.PermissionEntries),
            TotalCachedAclEntries = keys.AclEntries,
            TotalCachedPolicies = keys.PolicyEntries,
            CacheHitRate = cache.OverallHitRate,
            CacheSize = keys.TotalEntries,
            LastUpdated = DateTime.UtcNow,
            L1Hits = cache.L1Hits,
            L2Hits = cache.L2Hits,
            CacheMisses = cache.Misses,
            CacheEvictions = cache.Evictions,
            TotalRequests = cache.TotalRequests,
            L1HitRate = cache.L1HitRate,
            L2HitRate = cache.L2HitRate,
            ByType = cache.ByType.ToDictionary(
                pair => pair.Key,
                pair => new CacheTypeStatistics
                {
                    CacheType = pair.Value.CacheType,
                    L1Hits = pair.Value.L1Hits,
                    L2Hits = pair.Value.L2Hits,
                    Misses = pair.Value.Misses
                },
                StringComparer.Ordinal),
            PerformanceMetrics = cache.LookupDurationByType
                .Select(pair => new CachePerformanceMetric
                {
                    Operation = pair.Key,
                    AverageTime = pair.Value.AverageMilliseconds,
                    RequestCount = (int)Math.Min(int.MaxValue, pair.Value.Count),
                    Timestamp = DateTime.UtcNow
                })
                .ToList()
        });
    }
}

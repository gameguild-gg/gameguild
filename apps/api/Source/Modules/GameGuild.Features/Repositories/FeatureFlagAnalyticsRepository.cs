using Microsoft.EntityFrameworkCore;

namespace GameGuild.Features;

/// <summary>
///     Repository implementation for feature flag analytics and usage tracking
/// </summary>
public class FeatureFlagAnalyticsRepository : IFeatureFlagAnalyticsRepository
{
    private readonly IApplicationDbContext _context;

    public FeatureFlagAnalyticsRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordUsageAsync(FeatureFlagUsage usage, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlagUsage>().Add(usage);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlagUsage>> GetUsageAnalyticsAsync(string featureKey, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // First get the feature flag to find its ID
        var featureFlag = await _context.Set<FeatureFlag>()
            .FirstOrDefaultAsync(f => f.Key == featureKey && f.DeletedAt == null, cancellationToken).ConfigureAwait(false);

        if (featureFlag == null)
            return Enumerable.Empty<FeatureFlagUsage>();

        return await _context.Set<FeatureFlagUsage>()
            .Where(u => u.FeatureFlagId == featureFlag.Id &&
                       u.LastAccessAt >= startDate &&
                       u.LastAccessAt <= endDate &&
                       u.DeletedAt == null)
            .OrderByDescending(u => u.LastAccessAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlagUsage>> GetUsageByTenantAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagUsage>()
            .Where(u => u.TenantId == tenantId &&
                       u.LastAccessAt >= startDate &&
                       u.LastAccessAt <= endDate &&
                       u.DeletedAt == null)
            .OrderByDescending(u => u.LastAccessAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlagUsage>> GetUsageByUserAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagUsage>()
            .Where(u => u.UserId == userId &&
                       u.LastAccessAt >= startDate &&
                       u.LastAccessAt <= endDate &&
                       u.DeletedAt == null)
            .OrderByDescending(u => u.LastAccessAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeatureFlagUsageStats> GetAggregatedStatsAsync(string featureKey, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var featureFlag = await _context.Set<FeatureFlag>()
            .FirstOrDefaultAsync(f => f.Key == featureKey && f.DeletedAt == null, cancellationToken).ConfigureAwait(false);

        if (featureFlag == null)
        {
            return new FeatureFlagUsageStats
            {
                TotalAccessCount = 0,
                EnabledCount = 0,
                DisabledCount = 0,
                UniqueUserCount = 0,
                UniqueTenantCount = 0
            };
        }

        var usages = await _context.Set<FeatureFlagUsage>()
            .Where(u => u.FeatureFlagId == featureFlag.Id &&
                       u.LastAccessAt >= startDate &&
                       u.LastAccessAt <= endDate &&
                       u.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new FeatureFlagUsageStats
        {
            TotalAccessCount = usages.Sum(u => u.AccessCount),
            EnabledCount = usages.Count(u => u.WasEnabled),
            DisabledCount = usages.Count(u => !u.WasEnabled),
            UniqueUserCount = usages.Where(u => u.UserId.HasValue).Select(u => u.UserId).Distinct().Count(),
            UniqueTenantCount = usages.Where(u => u.TenantId.HasValue).Select(u => u.TenantId).Distinct().Count()
        };
    }

    public async Task<IEnumerable<string>> GetMostAccessedFeaturesAsync(int topCount, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var topFeatures = await _context.Set<FeatureFlagUsage>()
            .Where(u => u.LastAccessAt >= startDate && u.LastAccessAt <= endDate && u.DeletedAt == null)
            .GroupBy(u => u.FeatureFlagId)
            .Select(g => new
            {
                FeatureFlagId = g.Key,
                TotalAccess = g.Sum(u => u.AccessCount)
            })
            .OrderByDescending(x => x.TotalAccess)
            .Take(topCount)
            .ToListAsync(cancellationToken);

        var featureFlagIds = topFeatures.Select(x => x.FeatureFlagId).ToList();
        var featureFlags = await _context.Set<FeatureFlag>()
            .Where(f => featureFlagIds.Contains(f.Id) && f.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var featureFlagDict = featureFlags.ToDictionary(f => f.Id, f => f.Key);

        return topFeatures
            .Where(x => featureFlagDict.ContainsKey(x.FeatureFlagId))
            .Select(x => featureFlagDict[x.FeatureFlagId]);
    }

    public async Task RecordUsageBulkAsync(IEnumerable<FeatureFlagUsage> usageRecords, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlagUsage>().AddRange(usageRecords);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PurgeOldUsageRecordsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        var beforeDateOffset = new DateTimeOffset(beforeDate, TimeSpan.Zero);
        var oldRecords = await _context.Set<FeatureFlagUsage>()
            .Where(u => u.CreatedAt < beforeDateOffset && u.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var record in oldRecords)
        {
            record.SoftDelete();
        }

        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AnalyticsExportResult> ExportAnalyticsAsync(
        IEnumerable<string>? featureKeys,
        DateTime? startDate,
        DateTime? endDate,
        string format,
        bool includeDetails,
        string? groupBy,
        string? environment,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlagUsage>()
            .Where(u => u.DeletedAt == null);

        if (featureKeys != null && featureKeys.Any())
        {
            var featureFlags = await _context.Set<FeatureFlag>()
                .Where(f => featureKeys.Contains(f.Key) && f.DeletedAt == null)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var featureFlagIds = featureFlags.Select(f => f.Id).ToList();
            query = query.Where(u => featureFlagIds.Contains(u.FeatureFlagId));
        }

        if (startDate.HasValue)
            query = query.Where(u => u.LastAccessAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(u => u.LastAccessAt <= endDate.Value);

        if (!string.IsNullOrEmpty(environment))
            query = query.Where(u => u.Environment == environment);

        if (tenantId.HasValue)
            query = query.Where(u => u.TenantId == tenantId.Value);

        var usages = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Simple export result - serialize to JSON bytes
        var jsonData = System.Text.Json.JsonSerializer.Serialize<object>(includeDetails ? usages : usages.Select(u => new { u.FeatureFlagId, u.AccessCount, u.LastAccessAt }));
        return new AnalyticsExportResult
        {
            Content = System.Text.Encoding.UTF8.GetBytes(jsonData),
            ContentType = "application/json",
            FileName = $"feature-flag-analytics-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
            RecordCount = usages.Count,
            GeneratedAt = DateTime.UtcNow
        };
    }
}

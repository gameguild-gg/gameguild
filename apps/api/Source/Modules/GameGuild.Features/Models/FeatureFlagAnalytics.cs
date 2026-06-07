namespace GameGuild.Features;

/// <summary>
///     Feature flag analytics data
/// </summary>
public class FeatureFlagAnalytics
{
    public string FeatureKey { get; set; } = string.Empty;

    public long TotalAccesses { get; set; }

    public long EnabledAccesses { get; set; }

    public long DisabledAccesses { get; set; }

    public double EnabledPercentage { get; set; }

    public long UniqueUsers { get; set; }

    public long UniqueTenants { get; set; }

    public Dictionary<string, long> AccessesByTenant { get; init; } = [];

    public Dictionary<string, long> AccessesByEnvironment { get; init; } = [];

    public DateTime FirstAccess { get; set; }

    public DateTime LastAccess { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }
}

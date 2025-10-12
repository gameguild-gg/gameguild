namespace GameGuild.Modules.Features.Models;

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

    public Dictionary<string, long> AccessesByTenant { get; init; } = new Dictionary<string, long>();

    public Dictionary<string, long> AccessesByEnvironment { get; init; } = new Dictionary<string, long>();

    public DateTime FirstAccess { get; set; }

    public DateTime LastAccess { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }
}


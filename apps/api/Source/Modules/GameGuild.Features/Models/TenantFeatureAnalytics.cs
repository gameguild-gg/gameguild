namespace GameGuild.Features;

/// <summary>
///     Analytics data for tenant-specific feature usage
/// </summary>
public sealed class TenantFeatureAnalytics
{
    /// <summary>
    ///     Tenant identifier
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Total number of features accessed
    /// </summary>
    public int TotalFeaturesAccessed { get; set; }

    /// <summary>
    ///     Number of enabled features accessed
    /// </summary>
    public int EnabledFeaturesCount { get; set; }

    /// <summary>
    ///     Number of disabled features accessed
    /// </summary>
    public int DisabledFeaturesCount { get; set; }

    /// <summary>
    ///     Total access count across all features
    /// </summary>
    public long TotalAccessCount { get; set; }

    /// <summary>
    ///     Most accessed features for this tenant
    /// </summary>
    public IEnumerable<FeatureUsageRanking> TopFeatures { get; set; } = [];

    /// <summary>
    ///     First access date
    /// </summary>
    public DateTime? FirstAccessDate { get; set; }

    /// <summary>
    ///     Last access date
    /// </summary>
    public DateTime? LastAccessDate { get; set; }

    /// <summary>
    ///     Feature access breakdown by environment
    /// </summary>
    public IDictionary<string, long> AccessByEnvironment { get; set; } = new Dictionary<string, long>();
}

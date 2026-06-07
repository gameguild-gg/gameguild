namespace GameGuild.Features;

/// <summary>
///     Aggregated usage statistics for a feature flag
/// </summary>
public class FeatureFlagUsageStats
{
    /// <summary>
    ///     Total number of times the feature was accessed
    /// </summary>
    public long TotalAccessCount { get; set; }

    /// <summary>
    ///     Number of times the feature was enabled
    /// </summary>
    public long EnabledCount { get; set; }

    /// <summary>
    ///     Number of times the feature was disabled
    /// </summary>
    public long DisabledCount { get; set; }

    /// <summary>
    ///     Number of unique users who accessed the feature
    /// </summary>
    public int UniqueUserCount { get; set; }

    /// <summary>
    ///     Number of unique tenants who accessed the feature
    /// </summary>
    public int UniqueTenantCount { get; set; }

    /// <summary>
    ///     Percentage of times the feature was enabled (0-100)
    /// </summary>
    public double EnabledPercentage { get => TotalAccessCount > 0 ? (double) EnabledCount / TotalAccessCount * 100 : 0; }

    /// <summary>
    ///     First access date in the period
    /// </summary>
    public DateTime? FirstAccessDate { get; set; }

    /// <summary>
    ///     Last access date in the period
    /// </summary>
    public DateTime? LastAccessDate { get; set; }
}

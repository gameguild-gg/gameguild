namespace GameGuild.Features;

/// <summary>
///     Feature usage ranking information
/// </summary>
public sealed class FeatureUsageRanking
{
    /// <summary>
    ///     Feature key
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    ///     Total access count
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    ///     Number of times enabled
    /// </summary>
    public long EnabledCount { get; set; }

    /// <summary>
    ///     Number of times disabled
    /// </summary>
    public long DisabledCount { get; set; }

    /// <summary>
    ///     Number of unique users
    /// </summary>
    public int UniqueUserCount { get; set; }

    /// <summary>
    ///     Number of unique tenants
    /// </summary>
    public int UniqueTenantCount { get; set; }

    /// <summary>
    ///     Percentage enabled
    /// </summary>
    public double EnabledPercentage { get => AccessCount > 0 ? (double) EnabledCount / AccessCount * 100 : 0; }

    /// <summary>
    ///     Rank position
    /// </summary>
    public int Rank { get; set; }
}

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Plan comparison information
/// </summary>
public class PlanComparison
{
    /// <summary>
    ///     The plan being compared
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    ///     Plan name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Monthly price difference in cents (positive = more expensive, negative = cheaper)
    /// </summary>
    public long MonthlyPriceDifferenceInCents { get; set; }

    /// <summary>
    ///     Percentage price difference
    /// </summary>
    public decimal PriceDifferencePercentage { get; set; }

    /// <summary>
    ///     Additional users allowed (null = unlimited)
    /// </summary>
    public int? AdditionalUsers { get; set; }

    /// <summary>
    ///     Additional storage in MB (null = unlimited)
    /// </summary>
    public long? AdditionalStorageMb { get; set; }

    /// <summary>
    ///     Additional API calls per month (null = unlimited)
    /// </summary>
    public long? AdditionalApiCallsPerMonth { get; set; }

    /// <summary>
    ///     Additional features in the compared plan
    /// </summary>
    public List<string> AdditionalFeatures { get; set; } = new List<string>();

    /// <summary>
    ///     Whether this is an upgrade (true) or downgrade (false)
    /// </summary>
    public bool IsUpgrade { get; set; }
}

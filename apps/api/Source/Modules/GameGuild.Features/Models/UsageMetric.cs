namespace GameGuild.Features;

/// <summary>
///     Usage metric for a specific resource
/// </summary>
public class UsageMetric
{
    public string Name { get; init; } = string.Empty;

    public long CurrentUsage { get; init; }

    public long? Limit { get; init; }

    public double UtilizationPercentage { get => Limit.HasValue ? (double) CurrentUsage / Limit.Value * 100 : 0; }

    public bool IsOverLimit { get => CurrentUsage > Limit; }
}

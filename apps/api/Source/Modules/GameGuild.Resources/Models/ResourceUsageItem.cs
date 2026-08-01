namespace GameGuild.Resources;

/// <summary>
///     Resource usage item details
/// </summary>
public abstract class ResourceUsageItem
{
    public int Current { get; set; }

    public int Limit { get; set; }

    public DateTime Timestamp { get; set; }

    public long Amount { get; set; }

    public long PeakUsage { get; set; }

    public double PercentageUsed { get => Limit > 0 ? (double) Current / Limit * 100 : 0; }

    public bool IsLimitExceeded { get => Current >= Limit; }
}

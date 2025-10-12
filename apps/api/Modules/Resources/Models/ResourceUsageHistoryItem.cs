namespace GameGuild.Modules.Resources;

/// <summary> Historical usage item </summary>
public class ResourceUsageHistoryItem
{
    /// <summary> Date of usage </summary>
    public DateTime Date { get; set; }

    /// <summary> Usage count for that date </summary>
    public long Count { get; set; }

    /// <summary> Peak usage for that period </summary>
    public long? PeakUsage { get; set; }
}

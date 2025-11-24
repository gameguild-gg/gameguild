namespace GameGuild.Resources.Models;

/// <summary>
///     Resource usage history item for tracking over time
/// </summary>
public class ResourceUsageHistoryItem
{
    public DateTime Timestamp { get; set; }

    public long Amount { get; set; }

    public long? PeakUsage { get; set; }
}

namespace GameGuild.Modules.Resources;

/// <summary> Response containing resource usage information </summary>
public class ResourceUsageResponse
{
    /// <summary> Type of resource </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary> Current usage amount </summary>
    public long CurrentUsage { get; set; }

    /// <summary> Soft limit (if set) </summary>
    public long? SoftLimit { get; set; }

    /// <summary> Hard limit (if set) </summary>
    public long? HardLimit { get; set; }

    /// <summary> Usage percentage </summary>
    public double UsagePercentage { get; set; }

    /// <summary> Remaining quota </summary>
    public long? RemainingQuota { get; set; }

    /// <summary> Quota period </summary>
    public ResourceQuotaPeriod Period { get; set; }

    /// <summary> Last reset date </summary>
    public DateTime? LastReset { get; set; }

    /// <summary> Next reset date </summary>
    public DateTime? NextReset { get; set; }

    /// <summary> Whether quota is active </summary>
    public bool IsActive { get; set; }

    /// <summary> Historical usage data </summary>
    public List<ResourceUsageHistoryItem> History { get; set; } = [];
}

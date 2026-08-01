namespace GameGuild.Resources;

/// <summary>
///     Response model for current resource usage
/// </summary>
public class ResourceUsageResponse
{
    public Guid TenantId { get; set; }

    public Dictionary<string, ResourceUsageItem> Usage { get; set; } = new Dictionary<string, ResourceUsageItem>();

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public long CurrentUsage { get; set; }

    public double UsagePercentage { get; set; }

    public long RemainingQuota { get; set; }

    public List<ResourceUsageHistoryItem> History { get; set; } = [];
}

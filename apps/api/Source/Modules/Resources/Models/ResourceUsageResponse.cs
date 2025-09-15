namespace GameGuild.Modules.Resources.Models;

/// <summary>
/// Response containing resource usage information
/// </summary>
public class ResourceUsageResponse
{
    /// <summary>
    /// Type of resource
    /// </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary>
    /// Current usage amount
    /// </summary>
    public long CurrentUsage { get; set; }

    /// <summary>
    /// Soft limit (if set)
    /// </summary>
    public long? SoftLimit { get; set; }

    /// <summary>
    /// Hard limit (if set)
    /// </summary>
    public long? HardLimit { get; set; }

    /// <summary>
    /// Usage percentage
    /// </summary>
    public double UsagePercentage { get; set; }

    /// <summary>
    /// Remaining quota
    /// </summary>
    public long? RemainingQuota { get; set; }

    /// <summary>
    /// Quota period
    /// </summary>
    public ResourceQuotaPeriod Period { get; set; }

    /// <summary>
    /// Last reset date
    /// </summary>
    public DateTime? LastReset { get; set; }

    /// <summary>
    /// Next reset date
    /// </summary>
    public DateTime? NextReset { get; set; }

    /// <summary>
    /// Whether quota is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Historical usage data
    /// </summary>
    public List<ResourceUsageHistoryItem> History { get; set; } = new List<ResourceUsageHistoryItem>();
}

/// <summary>
/// Historical usage item
/// </summary>
public class ResourceUsageHistoryItem
{
    /// <summary>
    /// Date of usage
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Usage count for that date
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Peak usage for that period
    /// </summary>
    public long? PeakUsage { get; set; }
}

/// <summary>
/// Response containing multiple resource usage information
/// </summary>
public class MultiResourceUsageResponse
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Usage information for each resource type
    /// </summary>
    public Dictionary<ResourceUsageType, ResourceUsageResponse> Usage { get; set; } = new Dictionary<ResourceUsageType, ResourceUsageResponse>();

    /// <summary>
    /// Overall quota status
    /// </summary>
    public bool HasExceededLimits { get; set; }

    /// <summary>
    /// Resources that have exceeded soft limits
    /// </summary>
    public List<ResourceUsageType> SoftLimitExceeded { get; set; } = new List<ResourceUsageType>();

    /// <summary>
    /// Resources that have exceeded hard limits
    /// </summary>
    public List<ResourceUsageType> HardLimitExceeded { get; set; } = new List<ResourceUsageType>();

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

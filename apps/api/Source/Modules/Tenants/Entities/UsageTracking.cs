using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Resource types that can be tracked for usage
/// </summary>
public enum ResourceType
{
    Users,
    Storage,
    ApiCalls,
    Bandwidth,
    Compute,
    Database,
    Custom
}

/// <summary>
///     Tracks resource usage and limits for tenants
/// </summary>
[Table("usage_tracking")]
[Index(nameof(TenantId), nameof(ResourceType), IsUnique = true)]
[Index(nameof(LastUpdatedAt))]
public class UsageTracking : EntityBase
{
    /// <summary> ID of the tenant </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary> Navigation property to the tenant </summary>
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    /// <summary> Type of resource being tracked </summary>
    [Required]
    public ResourceType ResourceType { get; set; }

    /// <summary> Custom resource name (for ResourceType.Custom) </summary>
    [MaxLength(100)]
    public string? CustomResourceName { get; set; }

    /// <summary> Current usage amount </summary>
    public long CurrentUsage { get; set; }

    /// <summary> Usage limit (-1 for unlimited) </summary>
    public long UsageLimit { get; set; } = -1;

    /// <summary> Unit of measurement (e.g., "bytes", "calls", "users") </summary>
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    /// <summary> When the usage was last updated </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary> When the usage period started (for reset tracking) </summary>
    public DateTime PeriodStartedAt { get; set; } = DateTime.UtcNow;

    /// <summary> Check if usage limit is exceeded </summary>
    public bool IsLimitExceeded => UsageLimit != -1 && CurrentUsage >= UsageLimit;

    /// <summary> Calculate usage percentage (0-100) </summary>
    public decimal UsagePercentage => UsageLimit == -1 ? 0 : (decimal)CurrentUsage / UsageLimit * 100;

    /// <summary> Calculate remaining capacity </summary>
    public long RemainingCapacity => UsageLimit == -1 ? long.MaxValue : Math.Max(0, UsageLimit - CurrentUsage);

    /// <summary> Increment usage by amount </summary>
    public void IncrementUsage(long amount)
    {
        CurrentUsage += amount;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary> Reset usage to zero and start new period </summary>
    public void ResetUsage()
    {
        CurrentUsage = 0;
        PeriodStartedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary> Update the usage limit </summary>
    public void UpdateLimit(long newLimit)
    {
        UsageLimit = newLimit;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary> Check if within limit (with optional buffer percentage) </summary>
    public bool IsWithinLimit(decimal bufferPercentage = 0)
    {
        if (UsageLimit == -1) return true;
        var threshold = UsageLimit * (1 - bufferPercentage / 100);
        return CurrentUsage < threshold;
    }
}

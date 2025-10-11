using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Represents aggregated statistics for a tenant
///     Tracks usage metrics and resource consumption
/// </summary>
[Table("tenant_statistics")]
[Index(nameof(TenantId), IsUnique = true)]
[Index(nameof(LastUpdatedAt))]
public class TenantStatistics : EntityBase
{
    /// <summary> ID of the tenant these statistics belong to </summary>
    [Required]
    public override Guid TenantId { get; set; }

    /// <summary> Navigation property to the tenant </summary>
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    /// <summary> Total number of users associated with this tenant </summary>
    public int TotalUsers { get; set; }

    /// <summary> Number of currently active users </summary>
    public int ActiveUsers { get; set; }

    /// <summary> Total number of tenant members </summary>
    public int TotalMembers { get; set; }

    /// <summary> Number of active members </summary>
    public int ActiveMembers { get; set; }

    /// <summary> Total number of domains configured for this tenant </summary>
    public int TotalDomains { get; set; }

    /// <summary> Storage used by the tenant in bytes </summary>
    public long StorageUsedBytes { get; set; }

    /// <summary> Total number of API calls made </summary>
    public long TotalApiCalls { get; set; }

    /// <summary> Number of active subscriptions </summary>
    public int ActiveSubscriptions { get; set; }

    /// <summary> When these statistics were last updated </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary> Calculate storage used in megabytes </summary>
    public decimal StorageUsedMB => StorageUsedBytes / 1024m / 1024m;

    /// <summary> Calculate storage used in gigabytes </summary>
    public decimal StorageUsedGB => StorageUsedBytes / 1024m / 1024m / 1024m;

    /// <summary> Refresh the last updated timestamp </summary>
    public void RefreshTimestamp()
    {
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary> Update member counts </summary>
    public void UpdateMemberCounts(int total, int active)
    {
        TotalMembers = total;
        ActiveMembers = active;
        RefreshTimestamp();
    }

    /// <summary> Update user counts </summary>
    public void UpdateUserCounts(int total, int active)
    {
        TotalUsers = total;
        ActiveUsers = active;
        RefreshTimestamp();
    }

    /// <summary> Increment storage usage </summary>
    public void IncrementStorageUsage(long bytes)
    {
        StorageUsedBytes += bytes;
        RefreshTimestamp();
    }

    /// <summary> Increment API call count </summary>
    public void IncrementApiCalls(long count = 1)
    {
        TotalApiCalls += count;
        RefreshTimestamp();
    }
}

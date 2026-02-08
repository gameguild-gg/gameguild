using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Tenant statistics and usage metrics
///     Provides analytics and insights for tenant performance and usage
/// </summary>
[Table("TenantStatistics")]
[Index(nameof(TenantId))]
[Index(nameof(StatisticDate))]
public class TenantStatistics : EntityBase, ITenantable
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TenantStatistics() { }

    /// <summary>
    ///     ID of the tenant these statistics belong to
    /// </summary>
    [Required]
    public new Guid TenantId { get; set; }

    /// <summary>
    ///     Date for these statistics
    /// </summary>
    public DateTime StatisticDate { get; set; }

    /// <summary>
    ///     Total number of members
    /// </summary>
    public int TotalMembers { get; set; }

    /// <summary>
    ///     Number of active members
    /// </summary>
    public int ActiveMembers { get; set; }

    /// <summary>
    ///     Number of inactive members
    /// </summary>
    public int InactiveMembers { get; set; }

    /// <summary>
    ///     Total storage used in bytes
    /// </summary>
    public long StorageUsed { get; set; }

    /// <summary>
    ///     Number of API calls for this period
    /// </summary>
    public int ApiCalls { get; set; }

    /// <summary>
    ///     Number of new members added in this period
    /// </summary>
    public int NewMembers { get; set; }

    /// <summary>
    ///     Number of members who left in this period
    /// </summary>
    public int MembersLeft { get; set; }

    /// <summary>
    ///     Additional metrics as JSON
    /// </summary>
    [MaxLength(10000)]
    public string? CustomMetrics { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>
    ///     Update member statistics
    /// </summary>
    public void UpdateMemberStats(int total, int active, int inactive)
    {
        TotalMembers = total;
        ActiveMembers = active;
        InactiveMembers = inactive;
        Touch();
    }

    /// <summary>
    ///     Update storage usage
    /// </summary>
    public void UpdateStorageUsage(long storageUsed)
    {
        StorageUsed = storageUsed;
        Touch();
    }

    /// <summary>
    ///     Increment API call count
    /// </summary>
    public void IncrementApiCalls(int count = 1)
    {
        ApiCalls += count;
        Touch();
    }
}

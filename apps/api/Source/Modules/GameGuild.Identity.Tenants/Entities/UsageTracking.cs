using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Usage tracking for tenant resources and activities
///     Provides detailed tracking of tenant resource consumption
/// </summary>
[Table("UsageTracking")]
[Index(nameof(TenantId), nameof(Date))]
[Index(nameof(ResourceType))]
public class UsageTracking : EntityBase, ITenantable
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public UsageTracking() { }

    /// <summary>
    ///     ID of the tenant this usage tracking belongs to
    /// </summary>
    [Required]
    public new Guid TenantId { get; set; }

    /// <summary>
    ///     Date of usage
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    ///     Type of resource being tracked
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    ///     Amount of resource used
    /// </summary>
    public long UsageAmount { get; set; }

    /// <summary>
    ///     Unit of measurement (e.g., "bytes", "calls", "requests")
    /// </summary>
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    ///     Cost associated with this usage
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; }

    /// <summary>
    ///     Additional metadata as JSON
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>
    ///     Update usage amount
    /// </summary>
    public void UpdateUsage(long amount, decimal cost)
    {
        UsageAmount = amount;
        Cost = cost;
        Touch();
    }

    /// <summary>
    ///     Add to usage amount
    /// </summary>
    public void AddUsage(long amount, decimal additionalCost)
    {
        UsageAmount += amount;
        Cost += additionalCost;
        Touch();
    }
}

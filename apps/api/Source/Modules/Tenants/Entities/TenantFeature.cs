using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Represents a feature flag assignment for a tenant.
///     Enables/disables specific features per tenant for granular feature control.
/// </summary>
[Table("tenant_features")]
[Index(nameof(TenantId), nameof(FeatureKey), IsUnique = true)]
[Index(nameof(FeatureKey))]
[Index(nameof(IsEnabled))]
public class TenantFeature : EntityBase, ITenantable
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TenantFeature() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial tenant feature data</param>
    public TenantFeature(object partial) : base(partial) { }

    /// <summary>
    /// ID of the tenant this feature belongs to
    /// </summary>
    [Required]
    public new Guid? TenantId { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public override Tenant? Tenant { get; set; }

    /// <summary>
    ///     Unique key identifying the feature (e.g., "advanced_analytics", "custom_branding")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    ///     Human-readable name of the feature
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FeatureName { get; set; } = string.Empty;

    /// <summary>
    ///     Description of what the feature does
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    ///     Whether this feature is enabled for the tenant
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    ///     When the feature was enabled (null if never enabled)
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    ///     When the feature expires (null for unlimited)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Feature category/group (e.g., "Analytics", "Branding", "Integration")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    ///     Quota/limit for the feature (e.g., max users, max storage)
    /// </summary>
    public int? Quota { get; set; }

    /// <summary>
    ///     Current usage count for quota tracking
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    ///     Additional feature configuration (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    ///     Enable the feature
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        EnabledAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     Disable the feature
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        Touch();
    }

    /// <summary>
    ///     Set expiration date for the feature
    /// </summary>
    /// <param name="expiryDate">When the feature expires</param>
    public void SetExpiration(DateTime expiryDate)
    {
        ExpiresAt = expiryDate;
        Touch();
    }

    /// <summary>
    ///     Remove expiration (make unlimited)
    /// </summary>
    public void RemoveExpiration()
    {
        ExpiresAt = null;
        Touch();
    }

    /// <summary>
    ///     Update the quota limit
    /// </summary>
    /// <param name="newQuota">New quota value</param>
    public void UpdateQuota(int? newQuota)
    {
        Quota = newQuota;
        Touch();
    }

    /// <summary>
    ///     Increment usage count
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
        Touch();
    }

    /// <summary>
    ///     Reset usage count
    /// </summary>
    public void ResetUsage()
    {
        UsageCount = 0;
        Touch();
    }

    /// <summary>
    ///     Checks if the feature is currently valid (enabled and not expired)
    /// </summary>
    /// <returns>True if enabled and not expired</returns>
    public bool IsValid()
    {
        return IsEnabled && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    ///     Checks if quota has been exceeded
    /// </summary>
    /// <returns>True if usage exceeds quota</returns>
    public bool IsQuotaExceeded()
    {
        return Quota.HasValue && UsageCount >= Quota.Value;
    }

    /// <summary>
    ///     Checks if the feature is expiring soon (within specified days)
    /// </summary>
    /// <param name="days">Number of days threshold</param>
    /// <returns>True if expiring within the specified days</returns>
    public bool IsExpiringSoon(int days = 7)
    {
        if (ExpiresAt == null) return false;
        return ExpiresAt.Value <= DateTime.UtcNow.AddDays(days);
    }

    /// <summary>
    ///     Gets remaining quota
    /// </summary>
    /// <returns>Remaining quota or null if unlimited</returns>
    public int? GetRemainingQuota()
    {
        if (!Quota.HasValue) return null;
        return Math.Max(0, Quota.Value - UsageCount);
    }
}

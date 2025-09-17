using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Resources.Models;

/// <summary>
/// Defines resource usage quotas for tenants
/// </summary>
[Table("ResourceQuotas")]
[Index(nameof(TenantId), nameof(Type), IsUnique = true)]
public class ResourceQuota : EntityBase {
  /// <summary>
  /// Type of resource being limited
  /// </summary>
  public ResourceUsageType Type { get; set; }

  /// <summary>
  /// Tenant this quota applies to
  /// </summary>
  public Guid TenantId { get; set; }

  /// <summary>
  /// Soft limit amount (warning threshold)
  /// </summary>
  public long? SoftLimit { get; set; }

  /// <summary>
  /// Hard limit amount (enforcement threshold)
  /// </summary>
  public long? HardLimit { get; set; }

  /// <summary>
  /// Current usage count
  /// </summary>
  public long CurrentUsage { get; set; } = 0;

  /// <summary>
  /// Whether this quota is actively enforced
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Period type for quota reset (Monthly, Daily, etc.)
  /// </summary>
  public ResourceQuotaPeriod Period { get; set; } = ResourceQuotaPeriod.Monthly;

  /// <summary>
  /// Last time the quota was reset
  /// </summary>
  public DateTime? LastReset { get; set; }

  /// <summary>
  /// Optional time of day for quota resets (for daily/weekly periods)
  /// </summary>
  public TimeSpan? ResetTime { get; set; }

  /// <summary>
  /// Day of the week for weekly resets (0 = Sunday, 1 = Monday, etc.)
  /// </summary>
  public int? ResetDayOfWeek { get; set; }

  /// <summary>
  /// Day of the month for monthly resets (1-31)
  /// </summary>
  public int? ResetDayOfMonth { get; set; }

  /// <summary>
  /// Whether to send notifications when limits are approached
  /// </summary>
  public bool NotificationsEnabled { get; set; } = true;

  /// <summary>
  /// Percentage thresholds for notifications (e.g., "75,90,100")
  /// </summary>
  [MaxLength(100)]
  public string? NotificationThresholds { get; set; } = "75,90,100";

  /// <summary>
  /// Additional metadata about the quota (JSON)
  /// </summary>
  [MaxLength(2000)]
  public string? Metadata { get; set; }

  /// <summary>
  /// Calculate the percentage of quota used
  /// </summary>
  public double GetUsagePercentage() {
    if (!HardLimit.HasValue || HardLimit.Value == 0) return 0;

    return (double) CurrentUsage / HardLimit.Value * 100;
  }

  /// <summary>
  /// Check if soft limit is exceeded
  /// </summary>
  public bool IsSoftLimitExceeded() { return SoftLimit.HasValue && CurrentUsage >= SoftLimit.Value; }

  /// <summary>
  /// Check if hard limit is exceeded
  /// </summary>
  public bool IsHardLimitExceeded() { return HardLimit.HasValue && CurrentUsage >= HardLimit.Value; }

  /// <summary>
  /// Check if quota needs to be reset based on period
  /// </summary>
  public bool ShouldReset() {
    if (!LastReset.HasValue || Period == ResourceQuotaPeriod.Never) return false;

    var now = DateTime.UtcNow;
    var daysSinceReset = (now - LastReset.Value).TotalDays;

    return Period switch {
      ResourceQuotaPeriod.Daily => daysSinceReset >= 1,
      ResourceQuotaPeriod.Weekly => daysSinceReset >= 7,
      ResourceQuotaPeriod.Monthly => now.Month != LastReset.Value.Month || now.Year != LastReset.Value.Year,
      ResourceQuotaPeriod.Quarterly => (now.Year - LastReset.Value.Year) * 12 + now.Month - LastReset.Value.Month >= 3,
      ResourceQuotaPeriod.Yearly => now.Year != LastReset.Value.Year,
      _ => false
    };
  }

  /// <summary>
  /// Reset the quota usage
  /// </summary>
  public void Reset() {
    CurrentUsage = 0;
    LastReset = DateTime.UtcNow;
  }

  /// <summary>
  /// Increment usage by specified amount
  /// </summary>
  public bool TryIncrementUsage(long amount = 1) {
    if (IsHardLimitExceeded()) return false;

    CurrentUsage += amount;

    return true;
  }

  /// <summary>
  /// Get remaining quota amount
  /// </summary>
  public long? GetRemainingQuota() {
    if (!HardLimit.HasValue) return null;

    return Math.Max(0, HardLimit.Value - CurrentUsage);
  }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Features.Models;

/// <summary> Represents a feature flag in the system </summary>
[Table("FeatureFlags")]
[Index(nameof(Key), IsUnique = true)]
public class FeatureFlag : EntityBase {
  /// <summary> Unique key for the feature flag </summary>
  [Required]
  [MaxLength(100)]
  public string Key { get; set; } = string.Empty;

  /// <summary> Display name of the feature flag </summary>
  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = string.Empty;

  /// <summary> Description of what this feature flag controls </summary>
  [MaxLength(500)]
  public string Description { get; set; } = string.Empty;

  /// <summary> Whether this feature flag is currently enabled </summary>
  public bool IsEnabled { get; set; }

  /// <summary> Type of the feature flag </summary>
  public FeatureFlagType Type { get; set; } = FeatureFlagType.Toggle;

  /// <summary> Default value when the feature flag is disabled or not found </summary>
  [MaxLength(1000)]
  public string? DefaultValue { get; set; }

  /// <summary> Value when the feature flag is enabled </summary>
  [MaxLength(1000)]
  public string? EnabledValue { get; set; }

  /// <summary> Whether this feature flag applies to all tenants (global) </summary>
  public new bool IsGlobal { get; set; }

  /// <summary> Percentage rollout (0-100) for gradual feature releases </summary>
  [Range(0, 100)]
  public int RolloutPercentage { get; set; } = 100;

  /// <summary> Environment where this feature flag is active (e.g., "development", "staging", "production") </summary>
  [MaxLength(50)]
  public string Environment { get; set; } = "production";

  /// <summary> Tenant ID if this is a tenant-specific flag </summary>
  public Guid? TenantId { get; set; }

  /// <summary> Navigation property to feature flag targets </summary>
  public virtual ICollection<FeatureFlagTarget> Targets { get; init; } = new List<FeatureFlagTarget>();

  /// <summary> Navigation property to feature flag usage analytics </summary>
  public virtual ICollection<FeatureFlagUsage> UsageAnalytics { get; init; } = new List<FeatureFlagUsage>();
}

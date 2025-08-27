using GameGuild.Common;
using GameGuild.Modules.Tenants;


namespace GameGuild.Modules.TestingLab;

/// <summary> Represents the settings and configuration for the Testing Lab Can be global (TenantId = null) or tenant-specific (TenantId = specific value) </summary>
public class TestingLabSettings : Entity, ITenantable {
  /// <summary> Name of the testing lab </summary>
  [Required]
  [MaxLength(255)]
  public string LabName { get; set; } = "Testing Lab";

  /// <summary> Description of the testing lab </summary>
  [MaxLength(1000)]
  public string? Description { get; set; }

  /// <summary> Timezone for the testing lab (e.g., "UTC", "America/New_York") </summary>
  [Required]
  [MaxLength(50)]
  public string Timezone { get; set; } = "UTC";

  /// <summary> Default session duration in minutes </summary>
  [Required]
  [Range(15, 480)] // 15 minutes to 8 hours
  public int DefaultSessionDuration { get; set; } = 60;

  /// <summary> Whether to allow public signups for testing sessions </summary>
  public bool AllowPublicSignups { get; set; } = true;

  /// <summary> Whether manager approval is required for new testing participants </summary>
  public bool RequireApproval { get; set; } = true;

  /// <summary> Whether to enable email notifications for session updates </summary>
  public bool EnableNotifications { get; set; } = true;

  /// <summary> Maximum number of simultaneous sessions allowed </summary>
  [Required]
  [Range(1, 100)]
  public int MaxSimultaneousSessions { get; set; } = 10;

  /// <summary> Navigation property to the tenant this settings belongs to (implements ITenantable) If null, these are global settings that apply to all tenants If set, these are tenant-specific settings that override global ones </summary>
  public override Tenant? Tenant { get; set; }

  /// <summary> Indicates whether this settings is global (TenantId is null) or tenant-specific </summary>
  public override bool IsGlobal {
    get => Tenant == null;
  }
}

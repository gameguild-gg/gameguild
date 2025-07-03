using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.Reputation.Models;

/// <summary>
/// Configurable reputation tier definition stored in the database
/// Allows dynamic configuration of reputation thresholds and permissions
/// </summary>
[Table("ReputationLevels")]
[Index(nameof(MinimumScore))]
[Index(nameof(SortOrder))]
public class ReputationTier : ResourceBase, ITenantable {
  private string _name;

  private string _displayName;

  private int _minimumScore;

  private int? _maximumScore;

  private string? _color;

  private string? _icon;

  private int _sortOrder;

  private bool _isActive = true;

  private ICollection<UserReputation> _userReputations = new List<UserReputation>();

  private Tenant.Models.Tenant? _tenant;

  private Guid? _tenantId;

  /// <summary>
  /// Unique name/identifier for this reputation tier
  /// </summary>
  [Required]
  [MaxLength(100)]
  public required string Name {
    get => _name;
    [MemberNotNull(nameof(_name))] set => _name = value;
  }

  /// <summary>
  /// Display name for this reputation tier
  /// </summary>
  [Required]
  [MaxLength(200)]
  public required string DisplayName {
    get => _displayName;
    [MemberNotNull(nameof(_displayName))] set => _displayName = value;
  }

  /// <summary>
  /// Minimum score required to achieve this tier
  /// </summary>
  public int MinimumScore {
    get => _minimumScore;
    set => _minimumScore = value;
  }

  /// <summary>
  /// Maximum score for this tier (null means no upper limit)
  /// </summary>
  public int? MaximumScore {
    get => _maximumScore;
    set => _maximumScore = value;
  }

  /// <summary>
  /// Color or visual identifier for this tier (hex color, CSS class, etc.)
  /// </summary>
  [MaxLength(50)]
  public string? Color {
    get => _color;
    set => _color = value;
  }

  /// <summary>
  /// Icon or badge identifier for this tier
  /// </summary>
  [MaxLength(100)]
  public string? Icon {
    get => _icon;
    set => _icon = value;
  }

  /// <summary>
  /// Order/priority of this tier (lower numbers = higher priority)
  /// </summary>
  public int SortOrder {
    get => _sortOrder;
    set => _sortOrder = value;
  }

  /// <summary>
  /// Whether this tier is currently active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// Users who have achieved this reputation tier
  /// </summary>
  public ICollection<UserReputation> UserReputations {
    get => _userReputations;
    set => _userReputations = value;
  }

  /// <summary>
  /// Tenant this reputation tier belongs to (null for global tiers)
  /// </summary>
  [ForeignKey(nameof(TenantId))]
  public new Modules.Tenant.Models.Tenant? Tenant {
    get => _tenant;
    set => _tenant = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  /// <summary>
  /// Indicates whether this reputation tier is accessible across all tenants
  /// </summary>
  public new bool IsGlobal {
    get => TenantId == null;
  }
}

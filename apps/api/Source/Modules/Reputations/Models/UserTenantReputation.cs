using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;


namespace GameGuild.Modules.Reputations;

/// <summary>
/// Tracks a user's reputation score and tier within a specific tenant context
/// Supports a tenant-specific reputation that is separate from a global user reputation
/// </summary>
[Table("UserTenantReputations")]
[Index(nameof(TenantPermissionId), IsUnique = true)]
[Index(nameof(Score))]
[Index(nameof(CurrentLevelId))]
public class UserTenantReputation : Resource, IReputation {
  /// <summary>
  /// The user-tenant relationship this reputation belongs to
  /// </summary>
  public TenantPermission? TenantPermission { get; set; }

  public Guid? TenantPermissionId { get; set; }

  /// <summary>
  /// Current reputation score
  /// </summary>
  public int Score { get; set; }

  /// <summary>
  /// Current reputation tier (linked to configurable tier)
  /// </summary>
  public ReputationTier? CurrentLevel { get; set; }

  public Guid? CurrentLevelId { get; set; }

  /// <summary>
  /// When the reputation was last updated
  /// </summary>
  public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When the user's reputation tier was last recalculated
  /// </summary>
  public DateTime? LastLevelCalculation { get; set; }

  /// <summary>
  /// Number of positive reputation changes
  /// </summary>
  public int PositiveChanges { get; set; }

  /// <summary>
  /// Number of negative reputation changes
  /// </summary>
  public int NegativeChanges { get; set; }

  /// <summary>
  /// History of reputation changes for this user-tenant
  /// </summary>
  public ICollection<UserReputationHistory> History { get; set; } = new List<UserReputationHistory>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Models;


namespace GameGuild.Modules.Reputation.Models;

/// <summary>
/// Tracks a user's reputation score and tier within a specific tenant context
/// Supports tenant-specific reputation that is separate from global user reputation
/// </summary>
[Table("UserTenantReputations")]
[Index(nameof(TenantPermissionId), IsUnique = true)]
[Index(nameof(Score))]
[Index(nameof(CurrentLevelId))]
public class UserTenantReputation : ResourceBase, IReputation {
  private TenantPermission _tenantPermission;

  private Guid _tenantPermissionId;

  private int _score = 0;

  private ReputationTier? _currentLevel;

  private Guid? _currentLevelId;

  private DateTime _lastUpdated = DateTime.UtcNow;

  private DateTime? _lastLevelCalculation;

  private int _positiveChanges = 0;

  private int _negativeChanges = 0;

  private ICollection<UserReputationHistory> _history = new List<UserReputationHistory>();

  /// <summary>
  /// The user-tenant relationship this reputation belongs to
  /// </summary>
  [Required]
  [ForeignKey(nameof(TenantPermissionId))]
  public required Modules.Tenant.Models.TenantPermission TenantPermission {
    get => _tenantPermission;

    [MemberNotNull(nameof(_tenantPermission))]
    set => _tenantPermission = value;
  }

  [Required]
  public Guid TenantPermissionId {
    get => _tenantPermissionId;
    set => _tenantPermissionId = value;
  }

  /// <summary>
  /// Current reputation score
  /// </summary>
  public int Score {
    get => _score;
    set => _score = value;
  }

  /// <summary>
  /// Current reputation tier (linked to configurable tier)
  /// </summary>
  [ForeignKey(nameof(CurrentLevelId))]
  public ReputationTier? CurrentLevel {
    get => _currentLevel;
    set => _currentLevel = value;
  }

  public Guid? CurrentLevelId {
    get => _currentLevelId;
    set => _currentLevelId = value;
  }

  /// <summary>
  /// When the reputation was last updated
  /// </summary>
  public DateTime LastUpdated {
    get => _lastUpdated;
    set => _lastUpdated = value;
  }

  /// <summary>
  /// When the user's reputation tier was last recalculated
  /// </summary>
  public DateTime? LastLevelCalculation {
    get => _lastLevelCalculation;
    set => _lastLevelCalculation = value;
  }

  /// <summary>
  /// Number of positive reputation changes
  /// </summary>
  public int PositiveChanges {
    get => _positiveChanges;
    set => _positiveChanges = value;
  }

  /// <summary>
  /// Number of negative reputation changes
  /// </summary>
  public int NegativeChanges {
    get => _negativeChanges;
    set => _negativeChanges = value;
  }

  /// <summary>
  /// History of reputation changes for this user-tenant
  /// </summary>
  public ICollection<UserReputationHistory> History {
    get => _history;
    set => _history = value;
  }
}

public class UserTenantReputationConfiguration : IEntityTypeConfiguration<UserTenantReputation> {
  public void Configure(EntityTypeBuilder<UserTenantReputation> builder) {
    // Configure relationship with UserTenant (can't be done with annotations)
    builder.HasOne(utr => utr.TenantPermission)
           .WithMany()
           .HasForeignKey(utr => utr.TenantPermissionId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with CurrentLevel (can't be done with annotations)
    builder.HasOne(utr => utr.CurrentLevel)
           .WithMany()
           .HasForeignKey(utr => utr.CurrentLevelId)
           .OnDelete(DeleteBehavior.SetNull);
  }
}

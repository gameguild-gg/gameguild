using GameGuild.Modules.Resources;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Reputations;

/// <summary>
/// Tracks a user's reputation score and tier
/// </summary>
[Table("UserReputations")]
[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(Score))]
[Index(nameof(CurrentLevelId))]
public class UserReputation : Resource, IReputation {
  /// <summary>
  /// The user this reputation belongs to
  /// </summary>
  public User? User { get; set; }

  public Guid? UserId { get; set; }

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
  /// History of reputation changes for this user
  /// </summary>
  public ICollection<UserReputationHistory> History { get; set; } = new List<UserReputationHistory>();
}

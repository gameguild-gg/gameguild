using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Represents a user's earned achievement
/// </summary>
[Table("user_achievements")]
[Index(nameof(UserId))]
[Index(nameof(AchievementId))]
[Index(nameof(EarnedAt))]
[Index(nameof(TenantId))]
[Index(nameof(UserId), nameof(AchievementId), IsUnique = false)] // Allow repeatable achievements
public class UserAchievement : BaseEntity {
  /// <summary>
  /// The user who earned the achievement
  /// </summary>
  public Guid? UserId { get; set; }

  /// <summary>
  /// Navigation property to the user
  /// </summary>
  public virtual User? User { get; set; }

  /// <summary>
  /// The achievement that was earned
  /// </summary>
  public Guid AchievementId { get; set; }

  /// <summary>
  /// Navigation property to the achievement
  /// </summary>
  public virtual Achievement? Achievement { get; set; }

  /// <summary>
  /// When the achievement was earned
  /// </summary>
  public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// The level achieved if this is a multi-level achievement
  /// </summary>
  public int? Level { get; set; }

  /// <summary>
  /// Current progress towards this achievement (for partial completion tracking)
  /// </summary>
  public int Progress { get; set; } = 0;

  /// <summary>
  /// Maximum progress required to complete this achievement
  /// </summary>
  public int MaxProgress { get; set; } = 1;

  /// <summary>
  /// Whether the achievement has been completed
  /// </summary>
  public bool IsCompleted { get; set; } = false;

  /// <summary>
  /// Whether the user has been notified about earning this achievement
  /// </summary>
  public bool IsNotified { get; set; } = false;

  /// <summary>
  /// Additional context about how the achievement was earned (stored as JSON)
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? Context { get; set; }

  /// <summary>
  /// Points earned from this achievement (may differ from base achievement points based on level)
  /// </summary>
  public int PointsEarned { get; set; } = 0;

  /// <summary>
  /// The tenant this achievement belongs to
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Times this achievement has been earned (for repeatable achievements)
  /// </summary>
  public int EarnCount { get; set; } = 1;
}

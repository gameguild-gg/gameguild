using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserAchievements;

/// <summary> 
/// Represents a user's earned achievement with progress tracking and metadata.
/// Tracks completion status, notification state, and contextual information about how the achievement was earned.
/// Supports both one-time and repeatable achievements with level progression.
/// </summary>
[Table("user_achievements")]
[Index(nameof(UserId))]
[Index(nameof(AchievementId))]
[Index(nameof(EarnedAt))]
[Index(nameof(TenantId))]
[Index(nameof(UserId), nameof(AchievementId), IsUnique = false)] // Allow repeatable achievements
public class UserAchievement : EntityBase {
  /// <summary> The user who earned the achievement </summary>
  public Guid? UserId { get; set; }

  /// <summary> Navigation property to the user </summary>
  public virtual User? User { get; set; }

  /// <summary> The achievement that was earned </summary>
  public Guid AchievementId { get; set; }

  /// <summary> Navigation property to the achievement </summary>
  public virtual Achievement? Achievement { get; set; }

  /// <summary> When the achievement was earned </summary>
  public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

  /// <summary> The level achieved if this is a multi-level achievement </summary>
  public int? Level { get; set; }

  /// <summary> 
  /// Current progress towards this achievement (for partial completion tracking).
  /// Used for achievements that require multiple actions or incremental progress.
  /// </summary>
  public int Progress { get; set; } = 0;

  /// <summary> Maximum progress required to complete this achievement </summary>
  public int MaxProgress { get; set; } = 1;

  /// <summary> Whether the achievement has been completed </summary>
  public bool IsCompleted { get; set; } = false;

  /// <summary> Whether the user has been notified about earning this achievement </summary>
  public bool IsNotified { get; set; } = false;

  /// <summary> 
  /// Additional context about how the achievement was earned, stored as JSON.
  /// Examples: {"trigger": "post_created", "post_id": "123"}, {"streak_days": 7, "final_date": "2024-01-01"}
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? Context { get; set; }

  /// <summary> Points earned from this achievement (may differ from base achievement points based on level) </summary>
  public int PointsEarned { get; set; } = 0;

  /// <summary> The tenant this achievement belongs to </summary>
  public Guid? TenantId { get; set; }

  /// <summary> 
  /// Times this achievement has been earned (for repeatable achievements).
  /// Always 1 for non-repeatable achievements, can be > 1 for repeatable ones.
  /// </summary>
  public int EarnCount { get; set; } = 1;
}

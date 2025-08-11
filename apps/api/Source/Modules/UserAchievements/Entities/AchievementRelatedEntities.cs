using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Represents different levels for a multi-level achievement
/// </summary>
[Table("achievement_levels")]
[Index(nameof(AchievementId))]
[Index(nameof(Level))]
public class AchievementLevel : BaseEntity {
  /// <summary>
  /// The achievement this level belongs to
  /// </summary>
  public Guid AchievementId { get; set; }

  /// <summary>
  /// Navigation property to the achievement
  /// </summary>
  public virtual Achievement? Achievement { get; set; }

  /// <summary>
  /// The level number (1, 2, 3, etc.)
  /// </summary>
  public int Level { get; set; }

  /// <summary>
  /// Name for this level (e.g., "Bronze", "Silver", "Gold")
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of what this level represents
  /// </summary>
  [MaxLength(300)]
  public string? Description { get; set; }

  /// <summary>
  /// Points required to reach this level
  /// </summary>
  public int RequiredProgress { get; set; }

  /// <summary>
  /// Points awarded for reaching this level
  /// </summary>
  public int Points { get; set; }

  /// <summary>
  /// Icon specific to this level
  /// </summary>
  [MaxLength(255)]
  public string? IconUrl { get; set; }

  /// <summary>
  /// Color specific to this level
  /// </summary>
  [MaxLength(7)]
  public string? Color { get; set; }
}

/// <summary>
/// Represents prerequisites for earning an achievement
/// </summary>
[Table("achievement_prerequisites")]
[Index(nameof(AchievementId))]
[Index(nameof(PrerequisiteAchievementId))]
public class AchievementPrerequisite : BaseEntity {
  /// <summary>
  /// The achievement that has the prerequisite
  /// </summary>
  public Guid AchievementId { get; set; }

  /// <summary>
  /// Navigation property to the achievement
  /// </summary>
  public virtual Achievement? Achievement { get; set; }

  /// <summary>
  /// The achievement that must be earned first
  /// </summary>
  public Guid PrerequisiteAchievementId { get; set; }

  /// <summary>
  /// Navigation property to the prerequisite achievement
  /// </summary>
  public virtual Achievement? PrerequisiteAchievement { get; set; }

  /// <summary>
  /// Whether the prerequisite must be completed or just started
  /// </summary>
  public bool RequiresCompletion { get; set; } = true;

  /// <summary>
  /// Minimum level required if the prerequisite is a multi-level achievement
  /// </summary>
  public int? MinimumLevel { get; set; }
}

/// <summary>
/// Tracks user's progress towards achievements
/// </summary>
[Table("achievement_progress")]
[Index(nameof(UserId))]
[Index(nameof(AchievementId))]
[Index(nameof(TenantId))]
[Index(nameof(UserId), nameof(AchievementId), IsUnique = true)]
public class AchievementProgress : BaseEntity {
  /// <summary>
  /// The user making progress
  /// </summary>
  public Guid? UserId { get; set; }

  /// <summary>
  /// Navigation property to the user
  /// </summary>
  public virtual User? User { get; set; }

  /// <summary>
  /// The achievement being progressed towards
  /// </summary>
  public Guid AchievementId { get; set; }

  /// <summary>
  /// Navigation property to the achievement
  /// </summary>
  public virtual Achievement? Achievement { get; set; }

  /// <summary>
  /// Current progress value
  /// </summary>
  public int CurrentProgress { get; set; } = 0;

  /// <summary>
  /// Target progress required for completion
  /// </summary>
  public int TargetProgress { get; set; } = 1;

  /// <summary>
  /// When progress was last updated
  /// </summary>
  public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Whether this achievement has been completed
  /// </summary>
  public bool IsCompleted { get; set; } = false;

  /// <summary>
  /// Additional context data (stored as JSON)
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? Context { get; set; }

  /// <summary>
  /// The tenant this progress belongs to
  /// </summary>
  public Guid? TenantId { get; set; }
}

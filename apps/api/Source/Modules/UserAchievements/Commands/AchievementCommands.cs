using GameGuild.Common;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Command to create a new achievement
/// </summary>
public class CreateAchievementCommand : ICommand<GameGuild.Common.Result<Achievement>> {
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public string Category { get; set; } = string.Empty;
  public string Type { get; set; } = "badge";
  public string? IconUrl { get; set; }
  public string? Color { get; set; }
  public int Points { get; set; } = 0;
  public bool IsActive { get; set; } = true;
  public bool IsSecret { get; set; } = false;
  public bool IsRepeatable { get; set; } = false;
  public string? Conditions { get; set; }
  public int DisplayOrder { get; set; } = 0;
  public Guid? TenantId { get; set; }
  public List<CreateAchievementLevelCommand>? Levels { get; set; }
  public List<Guid>? PrerequisiteAchievementIds { get; set; }
}

/// <summary>
/// Command to create an achievement level
/// </summary>
public class CreateAchievementLevelCommand {
  public int Level { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public int RequiredProgress { get; set; }
  public int Points { get; set; }
  public string? IconUrl { get; set; }
  public string? Color { get; set; }
}

/// <summary>
/// Command to update an existing achievement
/// </summary>
public class UpdateAchievementCommand : ICommand<GameGuild.Common.Result<Achievement>> {
  public Guid AchievementId { get; set; }
  public string? Name { get; set; }
  public string? Description { get; set; }
  public string? Category { get; set; }
  public string? Type { get; set; }
  public string? IconUrl { get; set; }
  public string? Color { get; set; }
  public int? Points { get; set; }
  public bool? IsActive { get; set; }
  public bool? IsSecret { get; set; }
  public bool? IsRepeatable { get; set; }
  public string? Conditions { get; set; }
  public int? DisplayOrder { get; set; }
  public Guid UserId { get; set; } // For authorization
}

/// <summary>
/// Command to delete an achievement
/// </summary>
public class DeleteAchievementCommand : ICommand<Result> {
  public Guid AchievementId { get; set; }
  public Guid UserId { get; set; } // For authorization
}

/// <summary>
/// Command to award an achievement to a user
/// </summary>
public class AwardAchievementCommand : ICommand<GameGuild.Common.Result<UserAchievement>> {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public int? Level { get; set; }
  public int Progress { get; set; } = 1;
  public int MaxProgress { get; set; } = 1;
  public string? Context { get; set; }
  public bool NotifyUser { get; set; } = true;
  public Guid? TenantId { get; set; }
  public Guid? AwardedByUserId { get; set; } // For tracking who awarded it
}

/// <summary>
/// Command to update achievement progress for a user
/// </summary>
public class UpdateAchievementProgressCommand : ICommand<GameGuild.Common.Result<AchievementProgress>> {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public int ProgressIncrement { get; set; } = 1;
  public string? Context { get; set; }
  public bool AutoAward { get; set; } = true; // Automatically award if progress reaches target
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Command to revoke an achievement from a user
/// </summary>
public class RevokeAchievementCommand : ICommand<Result> {
  public Guid UserAchievementId { get; set; }
  public string? Reason { get; set; }
  public Guid RevokedByUserId { get; set; } // For auditing
}

/// <summary>
/// Command to bulk award achievements based on criteria
/// </summary>
public class BulkAwardAchievementCommand : ICommand<GameGuild.Common.Result<List<UserAchievement>>> {
  public Guid AchievementId { get; set; }
  public List<Guid>? UserIds { get; set; } // If null, applies to all eligible users
  public string? UserCriteria { get; set; } // JSON criteria for selecting users
  public string? Context { get; set; }
  public bool NotifyUsers { get; set; } = true;
  public Guid? TenantId { get; set; }
  public Guid AwardedByUserId { get; set; }
}

/// <summary>
/// Command to mark user achievement as notified
/// </summary>
public class MarkAchievementNotifiedCommand : ICommand<Result> {
  public Guid UserAchievementId { get; set; }
  public Guid UserId { get; set; } // For authorization
}

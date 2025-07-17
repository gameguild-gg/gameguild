namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Input type for creating achievements
/// </summary>
public class CreateAchievementInput {
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
  public List<CreateAchievementLevelInput>? Levels { get; set; }
  public List<Guid>? PrerequisiteAchievementIds { get; set; }
}

/// <summary>
/// Input type for creating achievement levels
/// </summary>
public class CreateAchievementLevelInput {
  public int Level { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public int RequiredProgress { get; set; }
  public int Points { get; set; }
  public string? IconUrl { get; set; }
  public string? Color { get; set; }
}

/// <summary>
/// Input type for updating achievements
/// </summary>
public class UpdateAchievementInput {
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
}

/// <summary>
/// Input type for awarding achievements
/// </summary>
public class AwardAchievementInput {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public int? Level { get; set; }
  public int Progress { get; set; } = 1;
  public int MaxProgress { get; set; } = 1;
  public string? Context { get; set; }
  public bool NotifyUser { get; set; } = true;
}

/// <summary>
/// Input type for updating achievement progress
/// </summary>
public class UpdateAchievementProgressInput {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public int ProgressIncrement { get; set; } = 1;
  public string? Context { get; set; }
  public bool AutoAward { get; set; } = true;
}

/// <summary>
/// Input type for revoking achievements
/// </summary>
public class RevokeAchievementInput {
  public Guid UserAchievementId { get; set; }
  public string? Reason { get; set; }
}

/// <summary>
/// Input type for bulk awarding achievements
/// </summary>
public class BulkAwardAchievementInput {
  public Guid AchievementId { get; set; }
  public List<Guid>? UserIds { get; set; }
  public string? UserCriteria { get; set; }
  public string? Context { get; set; }
  public bool NotifyUsers { get; set; } = true;
}

/// <summary>
/// Input type for achievement filters
/// </summary>
public class AchievementFilterInput {
  public string? Category { get; set; }
  public string? Type { get; set; }
  public bool? IsActive { get; set; }
  public bool? IsSecret { get; set; }
  public string? SearchTerm { get; set; }
}

/// <summary>
/// Input type for user achievement filters
/// </summary>
public class UserAchievementFilterInput {
  public string? Category { get; set; }
  public string? Type { get; set; }
  public bool? IsCompleted { get; set; }
  public DateTime? EarnedAfter { get; set; }
  public DateTime? EarnedBefore { get; set; }
}

/// <summary>
/// Input type for pagination
/// </summary>
public class PaginationInput {
  public int PageNumber { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string OrderBy { get; set; } = "CreatedAt";
  public bool Descending { get; set; } = true;
}

/// <summary>
/// Input type for leaderboard queries
/// </summary>
public class LeaderboardInput {
  public string? Category { get; set; }
  public int Limit { get; set; } = 50;
  public string OrderBy { get; set; } = "TotalPoints";
  public DateTime? TimeFrame { get; set; }
}

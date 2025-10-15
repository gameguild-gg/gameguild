namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// DTO for Achievement entity
/// </summary>
public class AchievementDto {
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public string Category { get; set; } = string.Empty;
  public string Type { get; set; } = string.Empty;
  public string? IconUrl { get; set; }
  public string? Color { get; set; }
  public int Points { get; set; }
  public bool IsActive { get; set; }
  public bool IsSecret { get; set; }
  public bool IsRepeatable { get; set; }
  public string? Conditions { get; set; }
  public int DisplayOrder { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public List<AchievementLevelDto>? Levels { get; set; }
  public List<AchievementDto>? Prerequisites { get; set; }
}

/// <summary>
/// DTO for UserAchievement entity
/// </summary>
public class UserAchievementDto {
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public AchievementDto? Achievement { get; set; }
  public DateTime EarnedAt { get; set; }
  public int? Level { get; set; }
  public int Progress { get; set; }
  public int MaxProgress { get; set; }
  public bool IsCompleted { get; set; }
  public bool IsNotified { get; set; }
  public string? Context { get; set; }
  public int PointsEarned { get; set; }
  public int EarnCount { get; set; }
}

/// <summary>
/// DTO for Achievement Level
/// </summary>
public class AchievementLevelDto {
  public Guid Id { get; set; }
  public int Level { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public int RequiredProgress { get; set; }
  public int Points { get; set; }
  public string? IconUrl { get; set; }
  public string? Color { get; set; }
}

/// <summary>
/// DTO for Achievement Progress
/// </summary>
public class AchievementProgressDto {
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public AchievementDto? Achievement { get; set; }
  public int CurrentProgress { get; set; }
  public int TargetProgress { get; set; }
  public double ProgressPercentage => TargetProgress > 0 ? (double)CurrentProgress / TargetProgress * 100 : 0;
  public DateTime LastUpdated { get; set; }
  public bool IsCompleted { get; set; }
  public string? Context { get; set; }
}

/// <summary>
/// Paginated response for achievements
/// </summary>
public class AchievementsPageDto {
  public List<AchievementDto> Achievements { get; set; } = new();
  public int TotalCount { get; set; }
  public int PageNumber { get; set; }
  public int PageSize { get; set; }
  public bool HasNextPage { get; set; }
  public bool HasPreviousPage { get; set; }

  public AchievementsPageDto(IEnumerable<AchievementDto> items, int totalCount, int pageNumber, int pageSize) {
    Achievements = items.ToList();
    TotalCount = totalCount;
    PageNumber = pageNumber;
    PageSize = pageSize;
    HasNextPage = pageNumber * pageSize < totalCount;
    HasPreviousPage = pageNumber > 1;
  }
}

/// <summary>
/// Paginated response for user achievements
/// </summary>
public class UserAchievementsPageDto {
  public List<UserAchievementDto> UserAchievements { get; set; } = new();
  public int TotalCount { get; set; }
  public int PageNumber { get; set; }
  public int PageSize { get; set; }
  public bool HasNextPage { get; set; }
  public bool HasPreviousPage { get; set; }

  public UserAchievementsPageDto(IEnumerable<UserAchievementDto> items, int totalCount, int pageNumber, int pageSize) {
    UserAchievements = items.ToList();
    TotalCount = totalCount;
    PageNumber = pageNumber;
    PageSize = pageSize;
    HasNextPage = pageNumber * pageSize < totalCount;
    HasPreviousPage = pageNumber > 1;
  }
}

/// <summary>
/// User achievement summary
/// </summary>
public class UserAchievementSummaryDto {
  public Guid UserId { get; set; }
  public int TotalAchievements { get; set; }
  public int TotalPoints { get; set; }
  public int CompletedAchievements { get; set; }
  public int InProgressAchievements { get; set; }
  public List<UserAchievementDto> RecentAchievements { get; set; } = new();
  public List<AchievementProgressDto> NearCompletion { get; set; } = new();
  public Dictionary<string, int> AchievementsByCategory { get; set; } = new();
}

/// <summary>
/// User achievement leaderboard entry
/// </summary>
public class UserAchievementLeaderboardDto {
  public int Rank { get; set; }
  public Guid UserId { get; set; }
  public string UserDisplayName { get; set; } = string.Empty;
  public int TotalAchievements { get; set; }
  public int TotalPoints { get; set; }
}

/// <summary>
/// Achievement statistics
/// </summary>
public class AchievementStatisticsDto {
  public Guid AchievementId { get; set; }
  public int TotalEarned { get; set; }
  public int TotalUsers { get; set; }
  public int InProgress { get; set; }
  public double CompletionRate { get; set; }
  public DateTime? FirstEarned { get; set; }
  public DateTime? LastEarned { get; set; }

  // Global statistics properties
  public int TotalAchievements { get; set; }
  public int ActiveAchievements { get; set; }
  public int SecretAchievements { get; set; }
  public int RepeatableAchievements { get; set; }
  public int UsersWithAchievements { get; set; }
  public int TotalAchievementsAwarded { get; set; }
  public Dictionary<string, int> AchievementsByCategory { get; set; } = new();
  public Dictionary<string, int> AchievementsByType { get; set; } = new();
  public List<AchievementPopularityDto> MostEarnedAchievements { get; set; } = new();
  public List<AchievementPopularityDto> RarestAchievements { get; set; } = new();
}

/// <summary>
/// DTO for prerequisite check result
/// </summary>
public class AchievementPrerequisiteCheckDto {
  public Guid AchievementId { get; set; }
  public bool CanEarn { get; set; }
  public List<PrerequisiteStatusDto> Prerequisites { get; set; } = new();
}

/// <summary>
/// DTO for prerequisite status
/// </summary>
public class PrerequisiteStatusDto {
  public Guid PrerequisiteAchievementId { get; set; }
  public string Name { get; set; } = string.Empty;
  public bool IsMet { get; set; }
  public bool RequiresCompletion { get; set; }
  public int? MinimumLevel { get; set; }
  public int? UserLevel { get; set; }
}

/// <summary>
/// DTO for achievement popularity information
/// </summary>
public class AchievementPopularityDto {
  public Guid AchievementId { get; set; }
  public string Name { get; set; } = string.Empty;
  public int TimesEarned { get; set; }
  public double EarnRate { get; set; }
}

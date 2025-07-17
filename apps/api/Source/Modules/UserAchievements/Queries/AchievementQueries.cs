using GameGuild.Common;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Query to get achievements with pagination and filtering
/// </summary>
public class GetAchievementsQuery : IQuery<Common.Result<AchievementsPageDto>> {
  public int PageNumber { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string? Category { get; set; }
  public string? Type { get; set; }
  public bool? IsActive { get; set; } = true;
  public bool? IsSecret { get; set; }
  public bool IncludeSecrets { get; set; } = false; // Admin flag to include secret achievements
  public string? SearchTerm { get; set; }
  public Guid? TenantId { get; set; }
  public string OrderBy { get; set; } = "DisplayOrder"; // DisplayOrder, Name, Points, CreatedAt
  public bool Descending { get; set; } = false;
}

/// <summary>
/// Query to get a single achievement by ID
/// </summary>
public class GetAchievementByIdQuery : IQuery<Common.Result<Achievement>> {
  public Guid AchievementId { get; set; }
  public bool IncludeLevels { get; set; } = true;
  public bool IncludePrerequisites { get; set; } = true;
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get user's achievements
/// </summary>
public class GetUserAchievementsQuery : IQuery<Common.Result<UserAchievementsPageDto>> {
  public Guid UserId { get; set; }
  public int PageNumber { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string? Category { get; set; }
  public string? Type { get; set; }
  public bool? IsCompleted { get; set; }
  public DateTime? EarnedAfter { get; set; }
  public DateTime? EarnedBefore { get; set; }
  public string OrderBy { get; set; } = "EarnedAt"; // EarnedAt, Points, AchievementName
  public bool Descending { get; set; } = true;
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get user's achievement progress
/// </summary>
public class GetUserAchievementProgressQuery : IQuery<Common.Result<List<AchievementProgressDto>>> {
  public Guid UserId { get; set; }
  public string? Category { get; set; }
  public bool OnlyInProgress { get; set; } = false; // Only show achievements with progress > 0 but not completed
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get user achievement summary
/// </summary>
public class GetUserAchievementSummaryQuery : IQuery<Common.Result<UserAchievementSummaryDto>> {
  public Guid UserId { get; set; }
  public int RecentLimit { get; set; } = 5; // Number of recent achievements to include
  public int NearCompletionThreshold { get; set; } = 80; // Percentage threshold for "near completion"
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get achievement leaderboard
/// </summary>
public class GetAchievementLeaderboardQuery : IQuery<Common.Result<List<UserAchievementLeaderboardDto>>> {
  public string? Category { get; set; }
  public int Limit { get; set; } = 50;
  public string OrderBy { get; set; } = "TotalPoints"; // TotalPoints, TotalAchievements, CompletedAchievements
  public DateTime? TimeFrame { get; set; } // Only count achievements earned after this date
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get achievement statistics
/// </summary>
public class GetAchievementStatisticsQuery : IQuery<Common.Result<AchievementStatisticsDto>> {
  public Guid AchievementId { get; set; } // Changed from nullable to required
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to check if user meets achievement prerequisites
/// </summary>
public class CheckAchievementPrerequisitesQuery : IQuery<Common.Result<AchievementPrerequisiteCheckDto>> {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get available achievements for a user (excluding already earned ones)
/// </summary>
public class GetAvailableAchievementsQuery : IQuery<Common.Result<AchievementsPageDto>> {
  public Guid UserId { get; set; }
  public int PageNumber { get; set; } = 1;
  public int PageSize { get; set; } = 20;
  public string? Category { get; set; }
  public bool ExcludeSecret { get; set; } = true; // Don't show secret achievements
  public bool OnlyEligible { get; set; } = false; // Only show achievements user is eligible for
  public Guid? TenantId { get; set; }
}


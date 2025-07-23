using GameGuild.Common;
using MediatR;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// GraphQL queries for achievements
/// </summary>
[ExtendObjectType<Query>]
public class AchievementQueries {
  /// <summary>
  /// Get achievements with pagination and filtering
  /// </summary>
  public async Task<AchievementsPageDto> GetAchievements(
    [Service] IMediator mediator,
    int pageNumber = 1,
    int pageSize = 20,
    string? category = null,
    string? type = null,
    bool? isActive = true,
    bool? isSecret = null,
    bool includeSecrets = false,
    string? searchTerm = null,
    string orderBy = "DisplayOrder",
    bool descending = false,
    Guid? tenantId = null) {
    var query = new GetAchievementsQuery {
      PageNumber = pageNumber,
      PageSize = pageSize,
      Category = category,
      Type = type,
      IsActive = isActive,
      IsSecret = isSecret,
      IncludeSecrets = includeSecrets,
      SearchTerm = searchTerm,
      OrderBy = orderBy,
      Descending = descending,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get achievement by ID
  /// </summary>
  public async Task<Achievement> GetAchievement(
    [Service] IMediator mediator,
    Guid achievementId,
    bool includeLevels = true,
    bool includePrerequisites = true,
    Guid? tenantId = null) {
    var query = new GetAchievementByIdQuery {
      AchievementId = achievementId,
      IncludeLevels = includeLevels,
      IncludePrerequisites = includePrerequisites,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get user's achievements
  /// </summary>
  public async Task<UserAchievementsPageDto> GetUserAchievements(
    [Service] IMediator mediator,
    Guid userId,
    int pageNumber = 1,
    int pageSize = 20,
    string? category = null,
    string? type = null,
    bool? isCompleted = null,
    DateTime? earnedAfter = null,
    DateTime? earnedBefore = null,
    string orderBy = "EarnedAt",
    bool descending = true,
    Guid? tenantId = null) {
    var query = new GetUserAchievementsQuery {
      UserId = userId,
      PageNumber = pageNumber,
      PageSize = pageSize,
      Category = category,
      Type = type,
      IsCompleted = isCompleted,
      EarnedAfter = earnedAfter,
      EarnedBefore = earnedBefore,
      OrderBy = orderBy,
      Descending = descending,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get user's achievement progress
  /// </summary>
  public async Task<List<AchievementProgressDto>> GetUserAchievementProgress(
    [Service] IMediator mediator,
    Guid userId,
    string? category = null,
    bool onlyInProgress = false,
    Guid? tenantId = null) {
    var query = new GetUserAchievementProgressQuery {
      UserId = userId,
      Category = category,
      OnlyInProgress = onlyInProgress,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get user's achievement summary
  /// </summary>
  public async Task<UserAchievementSummaryDto> GetUserAchievementSummary(
    [Service] IMediator mediator,
    Guid userId,
    int recentLimit = 5,
    int nearCompletionThreshold = 80,
    Guid? tenantId = null) {
    var query = new GetUserAchievementSummaryQuery {
      UserId = userId,
      RecentLimit = recentLimit,
      NearCompletionThreshold = nearCompletionThreshold,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get achievement leaderboard
  /// </summary>
  public async Task<List<UserAchievementLeaderboardDto>> GetAchievementLeaderboard(
    [Service] IMediator mediator,
    string? category = null,
    int limit = 50,
    string orderBy = "TotalPoints",
    DateTime? timeFrame = null,
    Guid? tenantId = null) {
    var query = new GetAchievementLeaderboardQuery {
      Category = category,
      Limit = limit,
      OrderBy = orderBy,
      TimeFrame = timeFrame,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get available achievements for a user
  /// </summary>
  public async Task<AchievementsPageDto> GetAvailableAchievements(
    [Service] IMediator mediator,
    Guid userId,
    int pageNumber = 1,
    int pageSize = 20,
    string? category = null,
    bool onlyEligible = false,
    Guid? tenantId = null) {
    var query = new GetAvailableAchievementsQuery {
      UserId = userId,
      PageNumber = pageNumber,
      PageSize = pageSize,
      Category = category,
      OnlyEligible = onlyEligible,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Check achievement prerequisites for a user
  /// </summary>
  public async Task<AchievementPrerequisiteCheckDto> CheckAchievementPrerequisites(
    [Service] IMediator mediator,
    Guid userId,
    Guid achievementId,
    Guid? tenantId = null) {
    var query = new CheckAchievementPrerequisitesQuery {
      UserId = userId,
      AchievementId = achievementId,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Get achievement statistics
  /// </summary>
  public async Task<AchievementStatisticsDto> GetAchievementStatistics(
    [Service] IMediator mediator,
    Guid achievementId,
    Guid? tenantId = null) {
    var query = new GetAchievementStatisticsQuery {
      AchievementId = achievementId,
      TenantId = tenantId,
    };

    var result = await mediator.Send(query);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }
}

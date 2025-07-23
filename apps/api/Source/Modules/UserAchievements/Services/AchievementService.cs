using GameGuild.Common;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Service interface for achievement management
/// </summary>
public interface IAchievementService {
  Task<GameGuild.Common.Result<UserAchievement>> AwardAchievementAsync(Guid userId, Guid achievementId, string? context = null, Guid? tenantId = null);
  Task<GameGuild.Common.Result<AchievementProgress>> UpdateProgressAsync(Guid userId, Guid achievementId, int progressIncrement = 1, string? context = null, Guid? tenantId = null);
  Task<GameGuild.Common.Result<List<Achievement>>> GetEligibleAchievementsAsync(Guid userId, Guid? tenantId = null);
  Task<GameGuild.Common.Result<bool>> CheckPrerequisitesAsync(Guid userId, Guid achievementId, Guid? tenantId = null);
  Task<GameGuild.Common.Result<List<UserAchievement>>> GetUnnotifiedAchievementsAsync(Guid userId, Guid? tenantId = null);
  Task<Result> MarkNotifiedAsync(Guid userAchievementId);
}

/// <summary>
/// Service for achievement management and gamification logic
/// </summary>
public class AchievementService : IAchievementService {
  private readonly ApplicationDbContext _context;
  private readonly IMediator _mediator;
  private readonly ILogger<AchievementService> _logger;

  public AchievementService(
    ApplicationDbContext context,
    IMediator mediator,
    ILogger<AchievementService> logger) {
    _context = context;
    _mediator = mediator;
    _logger = logger;
  }

  /// <summary>
  /// Award an achievement to a user
  /// </summary>
  public async Task<GameGuild.Common.Result<UserAchievement>> AwardAchievementAsync(
    Guid userId, 
    Guid achievementId, 
    string? context = null, 
    Guid? tenantId = null) {
    try {
      var command = new AwardAchievementCommand {
        UserId = userId,
        AchievementId = achievementId,
        Context = context,
        TenantId = tenantId,
      };

      return await _mediator.Send(command);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error awarding achievement {AchievementId} to user {UserId}", achievementId, userId);
      return GameGuild.Common.Result.Failure<UserAchievement>(GameGuild.Common.Error.Failure("AwardAchievement", "Failed to award achievement"));
    }
  }

  /// <summary>
  /// Update user's progress towards an achievement
  /// </summary>
  public async Task<GameGuild.Common.Result<AchievementProgress>> UpdateProgressAsync(
    Guid userId, 
    Guid achievementId, 
    int progressIncrement = 1, 
    string? context = null, 
    Guid? tenantId = null) {
    try {
      var command = new UpdateAchievementProgressCommand {
        UserId = userId,
        AchievementId = achievementId,
        ProgressIncrement = progressIncrement,
        Context = context,
        TenantId = tenantId,
      };

      return await _mediator.Send(command);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating progress for achievement {AchievementId} and user {UserId}", achievementId, userId);
      return GameGuild.Common.Result.Failure<AchievementProgress>(GameGuild.Common.Error.Failure("UpdateProgress", "Failed to update achievement progress"));
    }
  }

  /// <summary>
  /// Get achievements that a user is eligible to earn
  /// </summary>
  public async Task<GameGuild.Common.Result<List<Achievement>>> GetEligibleAchievementsAsync(Guid userId, Guid? tenantId = null) {
    try {
      // Get all active achievements for the tenant
      var allAchievements = await _context.Achievements
        .Include(a => a.Prerequisites)
        .Where(a => a.IsActive && a.TenantId == tenantId)
        .ToListAsync();

      // Get user's existing achievements
      var userAchievementIds = await _context.UserAchievements
        .Where(ua => ua.UserId == userId && ua.TenantId == tenantId)
        .Select(ua => ua.AchievementId)
        .ToListAsync();

      var eligibleAchievements = new List<Achievement>();

      foreach (var achievement in allAchievements) {
        // Skip if user already has this achievement (and it's not repeatable)
        if (!achievement.IsRepeatable && userAchievementIds.Contains(achievement.Id)) {
          continue;
        }

        // Check prerequisites
        var prerequisitesMet = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);
        if (prerequisitesMet) {
          eligibleAchievements.Add(achievement);
        }
      }

      return GameGuild.Common.Result.Success(eligibleAchievements);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting eligible achievements for user {UserId}", userId);
      return GameGuild.Common.Result.Failure<List<Achievement>>(GameGuild.Common.Error.Failure("GetEligibleAchievements", "Failed to get eligible achievements"));
    }
  }

  /// <summary>
  /// Check if a user meets the prerequisites for an achievement
  /// </summary>
  public async Task<GameGuild.Common.Result<bool>> CheckPrerequisitesAsync(Guid userId, Guid achievementId, Guid? tenantId = null) {
    try {
      var achievement = await _context.Achievements
        .Include(a => a.Prerequisites)
        .FirstOrDefaultAsync(a => a.Id == achievementId && a.TenantId == tenantId);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<bool>(GameGuild.Common.Error.NotFound("Achievement", "Achievement not found"));
      }

      var prerequisitesMet = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);
      return GameGuild.Common.Result.Success(prerequisitesMet);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error checking prerequisites for achievement {AchievementId} and user {UserId}", achievementId, userId);
      return GameGuild.Common.Result.Failure<bool>(GameGuild.Common.Error.Failure("CheckPrerequisites", "Failed to check prerequisites"));
    }
  }

  /// <summary>
  /// Get user's unnotified achievements
  /// </summary>
  public async Task<GameGuild.Common.Result<List<UserAchievement>>> GetUnnotifiedAchievementsAsync(Guid userId, Guid? tenantId = null) {
    try {
      var unnotifiedAchievements = await _context.UserAchievements
        .Include(ua => ua.Achievement)
        .Where(ua => ua.UserId == userId && 
                     ua.TenantId == tenantId && 
                     !ua.IsNotified && 
                     ua.IsCompleted)
        .ToListAsync();

      return GameGuild.Common.Result.Success(unnotifiedAchievements);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting unnotified achievements for user {UserId}", userId);
      return GameGuild.Common.Result.Failure<List<UserAchievement>>(GameGuild.Common.Error.Failure("GetUnnotifiedAchievements", "Failed to get unnotified achievements"));
    }
  }

  /// <summary>
  /// Mark an achievement as notified
  /// </summary>
  public async Task<Result> MarkNotifiedAsync(Guid userAchievementId) {
    try {
      var command = new MarkAchievementNotifiedCommand {
        UserAchievementId = userAchievementId,
        UserId = Guid.Empty, // This should be set from user context
      };

      return await _mediator.Send(command);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error marking achievement {UserAchievementId} as notified", userAchievementId);
      return GameGuild.Common.Result.Failure(GameGuild.Common.Error.Failure("MarkAsNotified", "Failed to mark achievement as notified"));
    }
  }

  /// <summary>
  /// Internal method to check prerequisites
  /// </summary>
  private async Task<bool> CheckPrerequisitesInternalAsync(Guid userId, Achievement achievement, Guid? tenantId) {
    if (!achievement.Prerequisites.Any()) {
      return true;
    }

    var userAchievements = await _context.UserAchievements
      .Where(ua => ua.UserId == userId && ua.TenantId == tenantId)
      .ToListAsync();

    foreach (var prerequisite in achievement.Prerequisites) {
      var hasPrerequisite = userAchievements.Any(ua => 
        ua.AchievementId == prerequisite.PrerequisiteAchievementId &&
        (!prerequisite.RequiresCompletion || ua.IsCompleted) &&
        (!prerequisite.MinimumLevel.HasValue || ua.Level >= prerequisite.MinimumLevel.Value));

      if (!hasPrerequisite) {
        return false;
      }
    }

    return true;
  }
}

/// <summary>
/// Service interface for achievement notifications
/// </summary>
public interface IAchievementNotificationService {
  Task NotifyAchievementEarnedAsync(UserAchievement userAchievement);
  Task NotifyProgressUpdateAsync(AchievementProgress progress);
  Task NotifyMilestoneReachedAsync(Guid userId, string milestoneType, int value);
}

/// <summary>
/// Service for sending achievement notifications
/// </summary>
public class AchievementNotificationService : IAchievementNotificationService {
  private readonly ILogger<AchievementNotificationService> _logger;

  public AchievementNotificationService(ILogger<AchievementNotificationService> logger) {
    _logger = logger;
  }

  /// <summary>
  /// Notify user about earned achievement
  /// </summary>
  public async Task NotifyAchievementEarnedAsync(UserAchievement userAchievement) {
    try {
      // Implementation would depend on your notification system
      // Could be email, push notifications, in-app notifications, etc.
      _logger.LogInformation("Achievement earned notification sent to user {UserId} for achievement {AchievementId}",
        userAchievement.UserId, userAchievement.AchievementId);

      await Task.CompletedTask; // Placeholder for actual notification logic
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error sending achievement earned notification");
    }
  }

  /// <summary>
  /// Notify user about progress update
  /// </summary>
  public async Task NotifyProgressUpdateAsync(AchievementProgress progress) {
    try {
      // Only notify on significant progress milestones
      var progressPercentage = progress.TargetProgress > 0 ? (double)progress.CurrentProgress / progress.TargetProgress * 100 : 0;
      
      if (progressPercentage >= 50 && progressPercentage < 100) {
        _logger.LogInformation("Progress notification sent to user {UserId} for achievement {AchievementId}: {Progress}%",
          progress.UserId, progress.AchievementId, progressPercentage);
      }

      await Task.CompletedTask; // Placeholder for actual notification logic
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error sending progress notification");
    }
  }

  /// <summary>
  /// Notify user about milestone reached
  /// </summary>
  public async Task NotifyMilestoneReachedAsync(Guid userId, string milestoneType, int value) {
    try {
      _logger.LogInformation("Milestone notification sent to user {UserId}: {MilestoneType} - {Value}",
        userId, milestoneType, value);

      await Task.CompletedTask; // Placeholder for actual notification logic
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error sending milestone notification");
    }
  }
}

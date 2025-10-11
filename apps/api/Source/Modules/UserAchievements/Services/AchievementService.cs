using GameGuild; // For Result and Error classes
using GameGuild.CQRS;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.UserAchievements;

/// <summary> 
/// Service interface for achievement management and gamification operations.
/// Provides high-level methods for awarding achievements, tracking progress, and managing notifications.
/// Abstracts complex business logic from controllers and handlers.
/// </summary>
public interface IAchievementService {
  /// <summary>
  /// Awards an achievement to a user if they meet the requirements.
  /// </summary>
  /// <param name="userId">The ID of the user to award the achievement to</param>
  /// <param name="achievementId">The ID of the achievement to award</param>
  /// <param name="context">Optional context information about how the achievement was earned (JSON format)</param>
  /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
  /// <returns>Result containing the user achievement record or error information</returns>
  Task<Result<UserAchievement>> AwardAchievementAsync(Guid userId, Guid achievementId, string? context = null, Guid? tenantId = null);

  /// <summary>
  /// Updates a user's progress towards an achievement and automatically awards it if completed.
  /// </summary>
  /// <param name="userId">The ID of the user whose progress to update</param>
  /// <param name="achievementId">The ID of the achievement to update progress for</param>
  /// <param name="progressIncrement">The amount to increase progress by (default: 1)</param>
  /// <param name="context">Optional context information about the progress update</param>
  /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
  /// <returns>Result containing the progress record or error information</returns>
  Task<Result<AchievementProgress>> UpdateProgressAsync(Guid userId, Guid achievementId, int progressIncrement = 1, string? context = null, Guid? tenantId = null);

  /// <summary>
  /// Retrieves achievements that a user is eligible to earn based on their current state and prerequisites.
  /// </summary>
  /// <param name="userId">The ID of the user to check eligibility for</param>
  /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
  /// <returns>Result containing list of eligible achievements or error information</returns>
  Task<Result<List<Achievement>>> GetEligibleAchievementsAsync(Guid userId, Guid? tenantId = null);

  /// <summary>
  /// Checks if a user meets all prerequisites for a specific achievement.
  /// </summary>
  /// <param name="userId">The ID of the user to check prerequisites for</param>
  /// <param name="achievementId">The ID of the achievement to validate prerequisites for</param>
  /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
  /// <returns>Result containing boolean indicating if prerequisites are met</returns>
  Task<Result<bool>> CheckPrerequisitesAsync(Guid userId, Guid achievementId, Guid? tenantId = null);

  /// <summary>
  /// Retrieves achievements that a user has earned but hasn't been notified about yet.
  /// Used by notification systems to show achievement alerts.
  /// </summary>
  /// <param name="userId">The ID of the user to check for unnotified achievements</param>
  /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
  /// <returns>Result containing list of unnotified user achievements</returns>
  Task<Result<List<UserAchievement>>> GetUnnotifiedAchievementsAsync(Guid userId, Guid? tenantId = null);

  /// <summary>
  /// Marks a user achievement as notified to prevent duplicate notifications.
  /// Should be called after successfully showing an achievement notification to the user.
  /// </summary>
  /// <param name="userAchievementId">The ID of the user achievement to mark as notified</param>
  /// <returns>Result indicating success or failure of the operation</returns>
  Task<Result> MarkNotifiedAsync(Guid userAchievementId);
}

/// <summary> 
/// Service implementation for achievement management and gamification logic.
/// Coordinates between handlers, validates business rules, and manages the achievement lifecycle.
/// Integrates with the CQRS pattern by delegating to appropriate command and query handlers.
/// </summary>
public class AchievementService : IAchievementService {
  private readonly ApplicationDbContext _context;

  private readonly ILogger<AchievementService> _logger;

  private readonly IMediator _mediator;

  /// <summary>
  /// Initializes a new instance of the AchievementService.
  /// </summary>
  /// <param name="context">Database context for data access</param>
  /// <param name="mediator">Mediator for CQRS pattern implementation</param>
  /// <param name="logger">Logger for diagnostic information</param>
  public AchievementService(ApplicationDbContext context, IMediator mediator, ILogger<AchievementService> logger) {
    _context = context;
    _mediator = mediator;
    _logger = logger;
  }

  /// <summary> 
  /// Award an achievement to a user by delegating to the appropriate command handler.
  /// Includes error handling and logging for audit trails.
  /// </summary>
  public async Task<Result<UserAchievement>> AwardAchievementAsync(Guid userId, Guid achievementId, string? context = null, Guid? tenantId = null) {
    try {
      var command = new AwardAchievementCommand { UserId = userId, AchievementId = achievementId, Context = context, TenantId = tenantId };

      return await _mediator.Send(command);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error awarding achievement {AchievementId} to user {UserId}", achievementId, userId);

      return Result.Failure<UserAchievement>(Error.Failure("AwardAchievement", "Failed to award achievement"));
    }
  }

  /// <summary> Update user's progress towards an achievement </summary>
  public async Task<Result<AchievementProgress>> UpdateProgressAsync(Guid userId, Guid achievementId, int progressIncrement = 1, string? context = null, Guid? tenantId = null) {
    try {
      var command = new UpdateAchievementProgressCommand { UserId = userId, AchievementId = achievementId, ProgressIncrement = progressIncrement, Context = context, TenantId = tenantId };

      return await _mediator.Send(command);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating progress for achievement {AchievementId} and user {UserId}", achievementId, userId);

      return Result.Failure<AchievementProgress>(Error.Failure("UpdateProgress", "Failed to update achievement progress"));
    }
  }

  /// <summary> Get achievements that a user is eligible to earn </summary>
  public async Task<Result<List<Achievement>>> GetEligibleAchievementsAsync(Guid userId, Guid? tenantId = null) {
    try {
      // Get all active achievements for the tenant
      var allAchievements = await _context.Achievements.Include(a => a.Prerequisites).Where(a => a.IsActive && a.TenantId == tenantId).ToListAsync();

      // Get user's existing achievements
      var userAchievementIds = await _context.UserAchievements.Where(ua => ua.UserId == userId && ua.TenantId == tenantId).Select(ua => ua.AchievementId).ToListAsync();

      var eligibleAchievements = new List<Achievement>();

      foreach (var achievement in allAchievements) {
        // Skip if user already has this achievement (and it's not repeatable)
        if (!achievement.IsRepeatable && userAchievementIds.Contains(achievement.Id)) { continue; }

        // Check prerequisites
        var prerequisitesMet = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);

        if (prerequisitesMet) { eligibleAchievements.Add(achievement); }
      }

      return Result.Success(eligibleAchievements);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting eligible achievements for user {UserId}", userId);

      return Result.Failure<List<Achievement>>(Error.Failure("GetEligibleAchievements", "Failed to get eligible achievements"));
    }
  }

  /// <summary> Check if a user meets the prerequisites for an achievement </summary>
  public async Task<Result<bool>> CheckPrerequisitesAsync(Guid userId, Guid achievementId, Guid? tenantId = null) {
    try {
      // Load the achievement with its prerequisite chain
      var achievement = await _context.Achievements.Include(a => a.Prerequisites).FirstOrDefaultAsync(a => a.Id == achievementId && a.TenantId == tenantId);

      if (achievement == null) { return Result.Failure<bool>(Error.NotFound("Achievement", "Achievement not found")); }

      // Recursively check all prerequisite achievements
      var prerequisitesMet = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);

      return Result.Success(prerequisitesMet);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error checking prerequisites for achievement {AchievementId} and user {UserId}", achievementId, userId);

      return Result.Failure<bool>(Error.Failure("CheckPrerequisites", "Failed to check prerequisites"));
    }
  }

  /// <summary> Get user's unnotified achievements </summary>
  public async Task<Result<List<UserAchievement>>> GetUnnotifiedAchievementsAsync(Guid userId, Guid? tenantId = null) {
    try {
      // Find completed achievements that haven't been shown to the user yet
      // This is used by notification systems to display achievement alerts
      var unnotifiedAchievements = await _context.UserAchievements.Include(ua => ua.Achievement).Where(ua => ua.UserId == userId && ua.TenantId == tenantId && !ua.IsNotified && ua.IsCompleted).ToListAsync();

      return Result.Success(unnotifiedAchievements);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting unnotified achievements for user {UserId}", userId);

      return Result.Failure<List<UserAchievement>>(Error.Failure("GetUnnotifiedAchievements", "Failed to get unnotified achievements"));
    }
  }

  /// <summary> Mark an achievement as notified </summary>
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

      return Result.Failure(Error.Failure("MarkAsNotified", "Failed to mark achievement as notified"));
    }
  }

  /// <summary> Internal method to check prerequisites </summary>
  private async Task<bool> CheckPrerequisitesInternalAsync(Guid userId, Achievement achievement, Guid? tenantId) {
    if (!achievement.Prerequisites.Any()) { return true; }

    var userAchievements = await _context.UserAchievements.Where(ua => ua.UserId == userId && ua.TenantId == tenantId).ToListAsync();

    foreach (var prerequisite in achievement.Prerequisites) {
      var hasPrerequisite = userAchievements.Any(ua => ua.AchievementId == prerequisite.PrerequisiteAchievementId &&
                                                       (!prerequisite.RequiresCompletion || ua.IsCompleted) &&
                                                       (!prerequisite.MinimumLevel.HasValue || ua.Level >= prerequisite.MinimumLevel.Value)
      );

      if (!hasPrerequisite) { return false; }
    }

    return true;
  }
}

/// <summary> Service interface for achievement notifications </summary>
public interface IAchievementNotificationService {
  Task NotifyAchievementEarnedAsync(UserAchievement userAchievement);

  Task NotifyProgressUpdateAsync(AchievementProgress progress);

  Task NotifyMilestoneReachedAsync(Guid userId, string milestoneType, int value);
}

/// <summary> Service for sending achievement notifications </summary>
public class AchievementNotificationService : IAchievementNotificationService {
  private readonly ILogger<AchievementNotificationService> _logger;

  public AchievementNotificationService(ILogger<AchievementNotificationService> logger) { _logger = logger; }

  /// <summary> Notify user about earned achievement </summary>
  public async Task NotifyAchievementEarnedAsync(UserAchievement userAchievement) {
    try {
      // Implementation would integrate with your notification system
      // Examples: email service, push notification service, real-time signaling, etc.
      // This is a placeholder that would be replaced with actual notification logic
      _logger.LogInformation("Achievement earned notification sent to user {UserId} for achievement {AchievementId}", userAchievement.UserId, userAchievement.AchievementId);

      await Task.CompletedTask; // Placeholder for actual notification logic
    }
    catch (Exception ex) { _logger.LogError(ex, "Error sending achievement earned notification"); }
  }

  /// <summary> Notify user about progress update </summary>
  public async Task NotifyProgressUpdateAsync(AchievementProgress progress) {
    try {
      // Calculate progress percentage to determine if notification is warranted
      // Only notify on significant milestones to avoid notification spam
      var progressPercentage = progress.TargetProgress > 0 ? (double)progress.CurrentProgress / progress.TargetProgress * 100 : 0;

      // Notify when user reaches 50% progress milestone (configurable threshold)
      if (progressPercentage >= 50 && progressPercentage < 100) {
        _logger.LogInformation("Progress notification sent to user {UserId} for achievement {AchievementId}: {Progress}%", progress.UserId, progress.AchievementId, progressPercentage);
      }

      await Task.CompletedTask; // Placeholder for actual notification logic
    }
    catch (Exception ex) { _logger.LogError(ex, "Error sending progress notification"); }
  }

  /// <summary> Notify user about milestone reached </summary>
  public async Task NotifyMilestoneReachedAsync(Guid userId, string milestoneType, int value) {
    try {
      _logger.LogInformation("Milestone notification sent to user {UserId}: {MilestoneType} - {Value}", userId, milestoneType, value);

      await Task.CompletedTask; // Placeholder for actual notification logic
    }
    catch (Exception ex) { _logger.LogError(ex, "Error sending milestone notification"); }
  }
}

using GameGuild.Models;

namespace GameGuild.Gamification.Achievements;

/// <summary>
/// Service interface for achievement management and gamification operations.
/// Provides high-level methods for awarding achievements, tracking progress, and managing notifications.
/// </summary>
public interface IAchievementService
{
    /// <summary>
    /// Awards an achievement to a user if they meet the requirements.
    /// </summary>
    /// <param name="userId">The ID of the user to award the achievement to</param>
    /// <param name="achievementId">The ID of the achievement to award</param>
    /// <param name="context">Optional context information about how the achievement was earned (JSON format)</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
    /// <returns>Result containing the user achievement record or error information</returns>
    Task<Result<UserAchievement>> AwardAchievementAsync(
        Guid userId,
        Guid achievementId,
        string? context = null,
        Guid? tenantId = null);

    /// <summary>
    /// Updates a user's progress towards an achievement and automatically awards it if completed.
    /// </summary>
    /// <param name="userId">The ID of the user whose progress to update</param>
    /// <param name="achievementId">The ID of the achievement to update progress for</param>
    /// <param name="progressIncrement">The amount to increase progress by (default: 1)</param>
    /// <param name="context">Optional context information about the progress update</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
    /// <returns>Result containing the progress record or error information</returns>
    Task<Result<AchievementProgress>> UpdateProgressAsync(
        Guid userId,
        Guid achievementId,
        int progressIncrement = 1,
        string? context = null,
        Guid? tenantId = null);

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
    /// </summary>
    /// <param name="userId">The ID of the user to check for unnotified achievements</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
    /// <returns>Result containing list of unnotified user achievements</returns>
    Task<Result<List<UserAchievement>>> GetUnnotifiedAchievementsAsync(Guid userId, Guid? tenantId = null);

    /// <summary>
    /// Marks a user achievement as notified to prevent duplicate notifications.
    /// </summary>
    /// <param name="userAchievementId">The ID of the user achievement to mark as notified</param>
    /// <returns>Result indicating success or failure of the operation</returns>
    Task<Result> MarkNotifiedAsync(Guid userAchievementId);

    /// <summary>
    /// Gets an achievement by ID.
    /// </summary>
    Task<Achievement?> GetAchievementByIdAsync(Guid id);

    /// <summary>
    /// Gets all achievements with optional filtering.
    /// </summary>
    Task<IEnumerable<Achievement>> GetAchievementsAsync(
        string? category = null,
        bool? isActive = true,
        bool includeSecrets = false,
        Guid? tenantId = null);

    /// <summary>
    /// Gets a user's achievements.
    /// </summary>
    Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(
        Guid userId,
        string? category = null,
        Guid? tenantId = null);

    /// <summary>
    /// Gets a user's total points.
    /// </summary>
    Task<int> GetUserTotalPointsAsync(Guid userId, Guid? tenantId = null);

    /// <summary>
    /// Creates a new achievement.
    /// </summary>
    Task<Result<Achievement>> CreateAchievementAsync(Achievement achievement);

    /// <summary>
    /// Updates an existing achievement.
    /// </summary>
    Task<Result<Achievement>> UpdateAchievementAsync(Achievement achievement);

    /// <summary>
    /// Deletes an achievement.
    /// </summary>
    Task<Result> DeleteAchievementAsync(Guid achievementId);
}

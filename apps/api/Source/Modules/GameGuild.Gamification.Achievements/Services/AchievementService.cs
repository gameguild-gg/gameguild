using GameGuild.Abstractions;
using GameGuild.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Gamification.Achievements;

/// <summary>
/// Service implementation for achievement management and gamification logic.
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AchievementService> _logger;

    public AchievementService(IApplicationDbContext context, ILogger<AchievementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UserAchievement>> AwardAchievementAsync(
        Guid userId,
        Guid achievementId,
        string? context = null,
        Guid? tenantId = null)
    {
        try
        {
            var achievement = await GetAchievementByIdAsync(achievementId);
            if (achievement == null)
            {
                return Result.Failure<UserAchievement>(Error.NotFound("Achievement", "Achievement not found"));
            }

            if (!achievement.IsActive)
            {
                return Result.Failure<UserAchievement>(Error.Validation("Achievement", "Achievement is not active"));
            }

            // Check if already earned (for non-repeatable achievements)
            if (!achievement.IsRepeatable)
            {
                var existing = await _context.Set<UserAchievement>()
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);

                if (existing != null)
                {
                    return Result.Failure<UserAchievement>(Error.Conflict("Achievement", "Achievement already earned"));
                }
            }

            // Check prerequisites
            var prerequisitesMet = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);
            if (!prerequisitesMet)
            {
                return Result.Failure<UserAchievement>(Error.Validation("Achievement", "Prerequisites not met"));
            }

            var userAchievement = UserAchievement.Create(userId, achievementId, achievement.Points, context, tenantId);

            _context.Set<UserAchievement>().Add(userAchievement);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Achievement {AchievementId} awarded to user {UserId}. Points: {Points}",
                achievementId, userId, achievement.Points);

            return Result.Success(userAchievement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding achievement {AchievementId} to user {UserId}", achievementId, userId);
            return Result.Failure<UserAchievement>(Error.Failure("AwardAchievement", "Failed to award achievement"));
        }
    }

    public async Task<Result<AchievementProgress>> UpdateProgressAsync(
        Guid userId,
        Guid achievementId,
        int progressIncrement = 1,
        string? context = null,
        Guid? tenantId = null)
    {
        try
        {
            var achievement = await GetAchievementByIdAsync(achievementId);
            if (achievement == null)
            {
                return Result.Failure<AchievementProgress>(Error.NotFound("Achievement", "Achievement not found"));
            }

            // Get or create progress record
            var progress = await _context.Set<AchievementProgress>()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.AchievementId == achievementId);

            if (progress == null)
            {
                // Determine target progress from achievement conditions or levels
                var targetProgress = achievement.Levels.Any()
                    ? achievement.Levels.Max(l => l.RequiredProgress)
                    : 1;

                progress = AchievementProgress.Create(userId, achievementId, targetProgress, tenantId);
                _context.Set<AchievementProgress>().Add(progress);
            }

            progress.IncrementProgress(progressIncrement);
            progress.Context = context;

            await _context.SaveChangesAsync();

            // Auto-award if completed
            if (progress.IsCompleted)
            {
                await AwardAchievementAsync(userId, achievementId, context, tenantId);
            }

            _logger.LogInformation(
                "Progress updated for achievement {AchievementId}, user {UserId}. Progress: {Current}/{Target}",
                achievementId, userId, progress.CurrentProgress, progress.TargetProgress);

            return Result.Success(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for achievement {AchievementId}, user {UserId}", achievementId, userId);
            return Result.Failure<AchievementProgress>(Error.Failure("UpdateProgress", "Failed to update progress"));
        }
    }

    public async Task<Result<List<Achievement>>> GetEligibleAchievementsAsync(Guid userId, Guid? tenantId = null)
    {
        try
        {
            var allAchievements = await _context.Set<Achievement>()
                .Include(a => a.Prerequisites)
                .Where(a => a.IsActive && (a.TenantId == tenantId || a.TenantId == null))
                .ToListAsync();

            var userAchievementIds = await _context.Set<UserAchievement>()
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AchievementId)
                .ToListAsync();

            var eligibleAchievements = new List<Achievement>();

            foreach (var achievement in allAchievements)
            {
                if (!achievement.IsRepeatable && userAchievementIds.Contains(achievement.Id))
                    continue;

                var prerequisitesMet = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);
                if (prerequisitesMet)
                {
                    eligibleAchievements.Add(achievement);
                }
            }

            return Result.Success(eligibleAchievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eligible achievements for user {UserId}", userId);
            return Result.Failure<List<Achievement>>(Error.Failure("GetEligibleAchievements", "Failed to get eligible achievements"));
        }
    }

    public async Task<Result<bool>> CheckPrerequisitesAsync(Guid userId, Guid achievementId, Guid? tenantId = null)
    {
        try
        {
            var achievement = await _context.Set<Achievement>()
                .Include(a => a.Prerequisites)
                .FirstOrDefaultAsync(a => a.Id == achievementId);

            if (achievement == null)
            {
                return Result.Failure<bool>(Error.NotFound("Achievement", "Achievement not found"));
            }

            var met = await CheckPrerequisitesInternalAsync(userId, achievement, tenantId);
            return Result.Success(met);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking prerequisites for achievement {AchievementId}, user {UserId}", achievementId, userId);
            return Result.Failure<bool>(Error.Failure("CheckPrerequisites", "Failed to check prerequisites"));
        }
    }

    public async Task<Result<List<UserAchievement>>> GetUnnotifiedAchievementsAsync(Guid userId, Guid? tenantId = null)
    {
        try
        {
            var unnotified = await _context.Set<UserAchievement>()
                .Include(ua => ua.Achievement)
                .Where(ua => ua.UserId == userId
                    && !ua.IsNotified
                    && ua.IsCompleted
                    && (ua.TenantId == tenantId || ua.TenantId == null))
                .ToListAsync();

            return Result.Success(unnotified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unnotified achievements for user {UserId}", userId);
            return Result.Failure<List<UserAchievement>>(Error.Failure("GetUnnotified", "Failed to get unnotified achievements"));
        }
    }

    public async Task<Result> MarkNotifiedAsync(Guid userAchievementId)
    {
        try
        {
            var userAchievement = await _context.Set<UserAchievement>()
                .FirstOrDefaultAsync(ua => ua.Id == userAchievementId);

            if (userAchievement == null)
            {
                return Result.Failure(Error.NotFound("UserAchievement", "User achievement not found"));
            }

            userAchievement.MarkAsNotified();
            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking achievement {UserAchievementId} as notified", userAchievementId);
            return Result.Failure(Error.Failure("MarkNotified", "Failed to mark as notified"));
        }
    }

    public async Task<Achievement?> GetAchievementByIdAsync(Guid id)
    {
        return await _context.Set<Achievement>()
            .Include(a => a.Levels)
            .Include(a => a.Prerequisites)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Achievement>> GetAchievementsAsync(
        string? category = null,
        bool? isActive = true,
        bool includeSecrets = false,
        Guid? tenantId = null)
    {
        var query = _context.Set<Achievement>()
            .Include(a => a.Levels)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(a => a.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        if (!includeSecrets)
        {
            query = query.Where(a => !a.IsSecret);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(a => a.TenantId == tenantId || a.TenantId == null);
        }

        return await query.OrderBy(a => a.DisplayOrder).ToListAsync();
    }

    public async Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(
        Guid userId,
        string? category = null,
        Guid? tenantId = null)
    {
        var query = _context.Set<UserAchievement>()
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(ua => ua.Achievement != null && ua.Achievement.Category == category);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(ua => ua.TenantId == tenantId || ua.TenantId == null);
        }

        return await query.OrderByDescending(ua => ua.EarnedAt).ToListAsync();
    }

    public async Task<int> GetUserTotalPointsAsync(Guid userId, Guid? tenantId = null)
    {
        return await _context.Set<UserAchievement>()
            .Where(ua => ua.UserId == userId && (ua.TenantId == tenantId || ua.TenantId == null))
            .SumAsync(ua => ua.PointsEarned);
    }

    public async Task<Result<Achievement>> CreateAchievementAsync(Achievement achievement)
    {
        try
        {
            _context.Set<Achievement>().Add(achievement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Achievement created: {AchievementId} - {Name}", achievement.Id, achievement.Name);

            return Result.Success(achievement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating achievement {Name}", achievement.Name);
            return Result.Failure<Achievement>(Error.Failure("CreateAchievement", "Failed to create achievement"));
        }
    }

    public async Task<Result<Achievement>> UpdateAchievementAsync(Achievement achievement)
    {
        try
        {
            _context.Set<Achievement>().Update(achievement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Achievement updated: {AchievementId}", achievement.Id);

            return Result.Success(achievement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating achievement {AchievementId}", achievement.Id);
            return Result.Failure<Achievement>(Error.Failure("UpdateAchievement", "Failed to update achievement"));
        }
    }

    public async Task<Result> DeleteAchievementAsync(Guid achievementId)
    {
        try
        {
            var achievement = await GetAchievementByIdAsync(achievementId);
            if (achievement == null)
            {
                return Result.Failure(Error.NotFound("Achievement", "Achievement not found"));
            }

            _context.Set<Achievement>().Remove(achievement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Achievement deleted: {AchievementId}", achievementId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting achievement {AchievementId}", achievementId);
            return Result.Failure(Error.Failure("DeleteAchievement", "Failed to delete achievement"));
        }
    }

    private async Task<bool> CheckPrerequisitesInternalAsync(Guid userId, Achievement achievement, Guid? tenantId)
    {
        if (!achievement.Prerequisites.Any())
            return true;

        var userAchievements = await _context.Set<UserAchievement>()
            .Where(ua => ua.UserId == userId && (ua.TenantId == tenantId || ua.TenantId == null))
            .ToListAsync();

        foreach (var prerequisite in achievement.Prerequisites)
        {
            var hasPrerequisite = userAchievements.Any(ua =>
                ua.AchievementId == prerequisite.PrerequisiteAchievementId &&
                (!prerequisite.RequiresCompletion || ua.IsCompleted) &&
                (!prerequisite.MinimumLevel.HasValue || ua.Level >= prerequisite.MinimumLevel.Value)
            );

            if (!hasPrerequisite)
                return false;
        }

        return true;
    }
}

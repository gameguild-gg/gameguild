                                                                                                                                                        using GameGuild.Common;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Error = GameGuild.Common.Error;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Handler for awarding achievements to users
/// </summary>
public class AwardAchievementCommandHandler : IRequestHandler<AwardAchievementCommand, GameGuild.Common.Result<UserAchievement>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<AwardAchievementCommandHandler> _logger;
  private readonly IPublisher _publisher;

  public AwardAchievementCommandHandler(
    ApplicationDbContext context,
    ILogger<AwardAchievementCommandHandler> logger,
    IPublisher publisher) {
    _context = context;
    _logger = logger;
    _publisher = publisher;
  }

  public async Task<GameGuild.Common.Result<UserAchievement>> Handle(AwardAchievementCommand request, CancellationToken cancellationToken) {
    try {
      // Get the achievement
      var achievement = await _context.Achievements
        .Include(a => a.Levels)
        .FirstOrDefaultAsync(a => a.Id == request.AchievementId && a.IsActive, cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<UserAchievement>(Error.NotFound("Achievement", "Achievement not found or inactive"));
      }

      // Check if user already has this achievement (for non-repeatable achievements)
      if (!achievement.IsRepeatable) {
        var existingAchievement = await _context.UserAchievements
          .FirstOrDefaultAsync(ua => ua.UserId == request.UserId && ua.AchievementId == request.AchievementId,
            cancellationToken);

        if (existingAchievement != null) {
          return GameGuild.Common.Result.Failure<UserAchievement>(Error.Conflict("UserAchievement", "User already has this achievement"));
        }
      }

      // Calculate points based on level
      var pointsEarned = achievement.Points;
      if (request.Level.HasValue && achievement.Levels.Any()) {
        var level = achievement.Levels.FirstOrDefault(l => l.Level == request.Level.Value);
        if (level != null) {
          pointsEarned = level.Points;
        }
      }

      // Get current earn count for repeatable achievements
      var earnCount = 1;
      if (achievement.IsRepeatable) {
        earnCount = await _context.UserAchievements
          .Where(ua => ua.UserId == request.UserId && ua.AchievementId == request.AchievementId)
          .CountAsync(cancellationToken) + 1;
      }

      var userAchievement = new UserAchievement {
        UserId = request.UserId,
        AchievementId = request.AchievementId,
        EarnedAt = DateTime.UtcNow,
        Level = request.Level,
        Progress = request.Progress,
        MaxProgress = request.MaxProgress,
        IsCompleted = request.Progress >= request.MaxProgress,
        IsNotified = !request.NotifyUser,
        Context = request.Context,
        PointsEarned = pointsEarned,
        TenantId = request.TenantId,
        EarnCount = earnCount,
      };

      _context.UserAchievements.Add(userAchievement);

      // Update or create achievement progress
      var progress = await _context.AchievementProgress
        .FirstOrDefaultAsync(ap => ap.UserId == request.UserId && ap.AchievementId == request.AchievementId,
          cancellationToken);

      if (progress != null) {
        progress.CurrentProgress = request.Progress;
        progress.IsCompleted = userAchievement.IsCompleted;
        progress.LastUpdated = DateTime.UtcNow;
      }
      else {
        progress = new AchievementProgress {
          UserId = request.UserId,
          AchievementId = request.AchievementId,
          CurrentProgress = request.Progress,
          TargetProgress = request.MaxProgress,
          IsCompleted = userAchievement.IsCompleted,
          TenantId = request.TenantId,
        };
        _context.AchievementProgress.Add(progress);
      }

      await _context.SaveChangesAsync(cancellationToken);

      // Publish event
      await _publisher.Publish(new AchievementEarnedEvent(userAchievement.Id) {
        UserAchievementId = userAchievement.Id,
        UserId = request.UserId,
        AchievementId = request.AchievementId,
        AchievementName = achievement.Name,
        Category = achievement.Category,
        PointsEarned = pointsEarned,
        Level = request.Level,
        EarnedAt = userAchievement.EarnedAt,
        IsRepeatable = achievement.IsRepeatable,
        EarnCount = earnCount,
        Context = request.Context,
        TenantId = request.TenantId,
        AwardedByUserId = request.AwardedByUserId,
      }, cancellationToken);

      _logger.LogInformation("Awarded achievement {AchievementName} to user {UserId}", achievement.Name, request.UserId);

      return GameGuild.Common.Result.Success(userAchievement);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error awarding achievement {AchievementId} to user {UserId}", request.AchievementId, request.UserId);
      return GameGuild.Common.Result.Failure<UserAchievement>(Error.Failure("AwardAchievement", "Failed to award achievement"));
    }
  }
}

/// <summary>
/// Handler for updating achievement progress
/// </summary>
public class UpdateAchievementProgressCommandHandler : IRequestHandler<UpdateAchievementProgressCommand, GameGuild.Common.Result<AchievementProgress>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<UpdateAchievementProgressCommandHandler> _logger;
  private readonly IMediator _mediator;
  private readonly IPublisher _publisher;

  public UpdateAchievementProgressCommandHandler(
    ApplicationDbContext context,
    ILogger<UpdateAchievementProgressCommandHandler> logger,
    IMediator mediator,
    IPublisher publisher) {
    _context = context;
    _logger = logger;
    _mediator = mediator;
    _publisher = publisher;
  }

  public async Task<GameGuild.Common.Result<AchievementProgress>> Handle(UpdateAchievementProgressCommand request, CancellationToken cancellationToken) {
    try {
      var achievement = await _context.Achievements
        .FirstOrDefaultAsync(a => a.Id == request.AchievementId && a.IsActive, cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<AchievementProgress>(Error.NotFound("Achievement", "Achievement not found or inactive"));
      }

      // Get or create progress record
      var progress = await _context.AchievementProgress
        .FirstOrDefaultAsync(ap => ap.UserId == request.UserId && ap.AchievementId == request.AchievementId,
          cancellationToken);

      var wasCompleted = progress?.IsCompleted ?? false;
      var previousProgress = progress?.CurrentProgress ?? 0;

      if (progress == null) {
        progress = new AchievementProgress {
          UserId = request.UserId,
          AchievementId = request.AchievementId,
          CurrentProgress = 0,
          TargetProgress = 1, // Default target
          TenantId = request.TenantId,
        };
        _context.AchievementProgress.Add(progress);
      }

      progress.CurrentProgress += request.ProgressIncrement;
      progress.LastUpdated = DateTime.UtcNow;
      progress.Context = request.Context;

      // Check if achievement is now completed
      var isNowCompleted = progress.CurrentProgress >= progress.TargetProgress;
      progress.IsCompleted = isNowCompleted;

      await _context.SaveChangesAsync(cancellationToken);

      // Publish progress event
      await _publisher.Publish(new AchievementProgressUpdatedEvent(request.AchievementId) {
        UserId = request.UserId,
        AchievementId = request.AchievementId,
        AchievementName = achievement.Name,
        PreviousProgress = previousProgress,
        NewProgress = progress.CurrentProgress,
        TargetProgress = progress.TargetProgress,
        ProgressPercentage = progress.TargetProgress > 0 ? (double)progress.CurrentProgress / progress.TargetProgress * 100 : 0,
        IsCompleted = isNowCompleted,
        Context = request.Context,
        TenantId = request.TenantId,
      }, cancellationToken);

      // Auto-award if completed and not already awarded
      if (request.AutoAward && isNowCompleted && !wasCompleted) {
        var awardCommand = new AwardAchievementCommand {
          UserId = request.UserId,
          AchievementId = request.AchievementId,
          Progress = progress.CurrentProgress,
          MaxProgress = progress.TargetProgress,
          Context = request.Context,
          TenantId = request.TenantId,
        };

        await _mediator.Send(awardCommand, cancellationToken);
      }

      _logger.LogInformation("Updated achievement progress for user {UserId} on achievement {AchievementId}: {Progress}/{Target}",
        request.UserId, request.AchievementId, progress.CurrentProgress, progress.TargetProgress);

      return GameGuild.Common.Result.Success(progress);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating achievement progress for user {UserId} on achievement {AchievementId}",
        request.UserId, request.AchievementId);
      return GameGuild.Common.Result.Failure<AchievementProgress>(Error.Failure("UpdateAchievementProgress", "Failed to update achievement progress"));
    }
  }
}

/// <summary>
/// Handler for revoking achievements
/// </summary>
public class RevokeAchievementCommandHandler : IRequestHandler<RevokeAchievementCommand, Result> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<RevokeAchievementCommandHandler> _logger;
  private readonly IPublisher _publisher;

  public RevokeAchievementCommandHandler(
    ApplicationDbContext context,
    ILogger<RevokeAchievementCommandHandler> logger,
    IPublisher publisher) {
    _context = context;
    _logger = logger;
    _publisher = publisher;
  }

  public async Task<Result> Handle(RevokeAchievementCommand request, CancellationToken cancellationToken) {
    try {
      var userAchievement = await _context.UserAchievements
        .Include(ua => ua.Achievement)
        .FirstOrDefaultAsync(ua => ua.Id == request.UserAchievementId, cancellationToken);

      if (userAchievement == null) {
        return GameGuild.Common.Result.Failure(Error.NotFound("UserAchievement", "User achievement not found"));
      }

      var achievement = userAchievement.Achievement!;
      var pointsLost = userAchievement.PointsEarned;

      _context.UserAchievements.Remove(userAchievement);

      // Update progress to incomplete
      var progress = await _context.AchievementProgress
        .FirstOrDefaultAsync(ap => ap.UserId == userAchievement.UserId && ap.AchievementId == userAchievement.AchievementId,
          cancellationToken);

      if (progress != null) {
        progress.IsCompleted = false;
        progress.LastUpdated = DateTime.UtcNow;
      }

      await _context.SaveChangesAsync(cancellationToken);

      // Publish event
      await _publisher.Publish(new AchievementRevokedEvent(userAchievement.Id) {
        UserAchievementId = userAchievement.Id,
        UserId = userAchievement.UserId ?? Guid.Empty,
        AchievementId = userAchievement.AchievementId,
        AchievementName = achievement.Name,
        PointsLost = pointsLost,
        Reason = request.Reason,
        TenantId = userAchievement.TenantId,
        RevokedByUserId = request.RevokedByUserId,
      }, cancellationToken);

      _logger.LogInformation("Revoked achievement {AchievementName} from user {UserId}. Reason: {Reason}",
        achievement.Name, userAchievement.UserId, request.Reason);

      return GameGuild.Common.Result.Success();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error revoking user achievement {UserAchievementId}", request.UserAchievementId);
      return GameGuild.Common.Result.Failure(Error.Failure("RevokeAchievement", "Failed to revoke achievement"));
    }
  }
}

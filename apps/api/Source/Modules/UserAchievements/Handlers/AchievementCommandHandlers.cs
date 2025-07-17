using GameGuild.Common;
using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Handler for creating achievements
/// </summary>
public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, GameGuild.Common.Result<Achievement>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<CreateAchievementCommandHandler> _logger;
  private readonly IPublisher _publisher;

  public CreateAchievementCommandHandler(
    ApplicationDbContext context,
    ILogger<CreateAchievementCommandHandler> logger,
    IPublisher publisher) {
    _context = context;
    _logger = logger;
    _publisher = publisher;
  }

  public async Task<GameGuild.Common.Result<Achievement>> Handle(CreateAchievementCommand request, CancellationToken cancellationToken) {
    try {
      // Check if achievement with same name exists in tenant
      var existingAchievement = await _context.Achievements
        .Where(a => a.Name == request.Name && a.TenantId == request.TenantId)
        .FirstOrDefaultAsync(cancellationToken);

      if (existingAchievement != null) {
        return GameGuild.Common.Result.Failure<Achievement>(GameGuild.Common.Error.Conflict("Achievement", "Achievement with this name already exists"));
      }

      var achievement = new Achievement {
        Name = request.Name,
        Description = request.Description,
        Category = request.Category,
        Type = request.Type,
        IconUrl = request.IconUrl,
        Color = request.Color,
        Points = request.Points,
        IsActive = request.IsActive,
        IsSecret = request.IsSecret,
        IsRepeatable = request.IsRepeatable,
        Conditions = request.Conditions,
        DisplayOrder = request.DisplayOrder,
        TenantId = request.TenantId
      };

      _context.Achievements.Add(achievement);

      // Add levels if provided
      if (request.Levels?.Any() == true) {
        foreach (var levelRequest in request.Levels) {
          var level = new AchievementLevel {
            AchievementId = achievement.Id,
            Level = levelRequest.Level,
            Name = levelRequest.Name,
            Description = levelRequest.Description,
            RequiredProgress = levelRequest.RequiredProgress,
            Points = levelRequest.Points,
            IconUrl = levelRequest.IconUrl,
            Color = levelRequest.Color
          };
          _context.AchievementLevels.Add(level);
        }
      }

      // Add prerequisites if provided
      if (request.PrerequisiteAchievementIds?.Any() == true) {
        foreach (var prerequisiteId in request.PrerequisiteAchievementIds) {
          var prerequisite = new AchievementPrerequisite {
            AchievementId = achievement.Id,
            PrerequisiteAchievementId = prerequisiteId,
            RequiresCompletion = true
          };
          _context.AchievementPrerequisites.Add(prerequisite);
        }
      }

      await _context.SaveChangesAsync(cancellationToken);

      // Publish event
      await _publisher.Publish(new AchievementCreatedEvent(achievement.Id) {
        AchievementId = achievement.Id,
        Name = achievement.Name,
        Category = achievement.Category,
        Type = achievement.Type,
        Points = achievement.Points,
        TenantId = achievement.TenantId,
        CreatedByUserId = Guid.Empty // This should come from user context
      }, cancellationToken);

      _logger.LogInformation("Created achievement {AchievementName} with ID {AchievementId}", achievement.Name, achievement.Id);

      return GameGuild.Common.Result.Success(achievement);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error creating achievement {AchievementName}", request.Name);
      return GameGuild.Common.Result.Failure<Achievement>(GameGuild.Common.Error.Failure("CreateAchievement", "Failed to create achievement"));
    }
  }
}

/// <summary>
/// Handler for updating achievements
/// </summary>
public class UpdateAchievementCommandHandler : IRequestHandler<UpdateAchievementCommand, GameGuild.Common.Result<Achievement>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<UpdateAchievementCommandHandler> _logger;
  private readonly IPublisher _publisher;

  public UpdateAchievementCommandHandler(
    ApplicationDbContext context,
    ILogger<UpdateAchievementCommandHandler> logger,
    IPublisher publisher) {
    _context = context;
    _logger = logger;
    _publisher = publisher;
  }

  public async Task<GameGuild.Common.Result<Achievement>> Handle(UpdateAchievementCommand request, CancellationToken cancellationToken) {
    try {
      var achievement = await _context.Achievements
        .FirstOrDefaultAsync(a => a.Id == request.AchievementId, cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure<Achievement>(GameGuild.Common.Error.NotFound("Achievement", "Achievement not found"));
      }

      // Update only provided fields
      if (!string.IsNullOrEmpty(request.Name)) achievement.Name = request.Name;
      if (request.Description != null) achievement.Description = request.Description;
      if (!string.IsNullOrEmpty(request.Category)) achievement.Category = request.Category;
      if (!string.IsNullOrEmpty(request.Type)) achievement.Type = request.Type;
      if (request.IconUrl != null) achievement.IconUrl = request.IconUrl;
      if (request.Color != null) achievement.Color = request.Color;
      if (request.Points.HasValue) achievement.Points = request.Points.Value;
      if (request.IsActive.HasValue) achievement.IsActive = request.IsActive.Value;
      if (request.IsSecret.HasValue) achievement.IsSecret = request.IsSecret.Value;
      if (request.IsRepeatable.HasValue) achievement.IsRepeatable = request.IsRepeatable.Value;
      if (request.Conditions != null) achievement.Conditions = request.Conditions;
      if (request.DisplayOrder.HasValue) achievement.DisplayOrder = request.DisplayOrder.Value;

      achievement.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      // Publish event
      await _publisher.Publish(new AchievementUpdatedEvent(achievement.Id) {
        AchievementId = achievement.Id,
        Name = achievement.Name,
        IsActive = achievement.IsActive,
        TenantId = achievement.TenantId,
        UpdatedByUserId = request.UserId
      }, cancellationToken);

      _logger.LogInformation("Updated achievement {AchievementName} with ID {AchievementId}", achievement.Name, achievement.Id);

      return GameGuild.Common.Result.Success(achievement);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error updating achievement {AchievementId}", request.AchievementId);
      return GameGuild.Common.Result.Failure<Achievement>(GameGuild.Common.Error.Failure("UpdateAchievement", "Failed to update achievement"));
    }
  }
}

/// <summary>
/// Handler for deleting achievements
/// </summary>
public class DeleteAchievementCommandHandler : IRequestHandler<DeleteAchievementCommand, Result> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<DeleteAchievementCommandHandler> _logger;
  private readonly IPublisher _publisher;

  public DeleteAchievementCommandHandler(
    ApplicationDbContext context,
    ILogger<DeleteAchievementCommandHandler> logger,
    IPublisher publisher) {
    _context = context;
    _logger = logger;
    _publisher = publisher;
  }

  public async Task<Result> Handle(DeleteAchievementCommand request, CancellationToken cancellationToken) {
    try {
      var achievement = await _context.Achievements
        .FirstOrDefaultAsync(a => a.Id == request.AchievementId, cancellationToken);

      if (achievement == null) {
        return GameGuild.Common.Result.Failure(GameGuild.Common.Error.NotFound("Achievement", "Achievement not found"));
      }

      // Check if any users have earned this achievement
      var hasEarnedAchievements = await _context.UserAchievements
        .AnyAsync(ua => ua.AchievementId == request.AchievementId, cancellationToken);

      if (hasEarnedAchievements) {
        // Instead of deleting, mark as inactive
        achievement.IsActive = false;
        achievement.UpdatedAt = DateTime.UtcNow;
      }
      else {
        // Safe to delete if no one has earned it
        _context.Achievements.Remove(achievement);
      }

      await _context.SaveChangesAsync(cancellationToken);

      // Publish event
      await _publisher.Publish(new AchievementDeletedEvent(achievement.Id) {
        AchievementId = achievement.Id,
        Name = achievement.Name,
        TenantId = achievement.TenantId,
        DeletedByUserId = request.UserId
      }, cancellationToken);

      _logger.LogInformation("Deleted achievement {AchievementName} with ID {AchievementId}", achievement.Name, achievement.Id);

      return GameGuild.Common.Result.Success();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error deleting achievement {AchievementId}", request.AchievementId);
      return GameGuild.Common.Result.Failure(GameGuild.Common.Error.Failure("DeleteAchievement", "Failed to delete achievement"));
    }
  }
}

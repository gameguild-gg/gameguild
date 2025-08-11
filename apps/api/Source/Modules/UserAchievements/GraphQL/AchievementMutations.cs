using GameGuild.Common;
using HotChocolate.Authorization;
using MediatR;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// GraphQL mutations for achievements
/// </summary>
[ExtendObjectType<Mutation>]
public class AchievementMutations {
  /// <summary>
  /// Create a new achievement
  /// </summary>
  [Authorize(Roles = new[] { "Admin", "Moderator" })]
  public async Task<Achievement> CreateAchievement(
    [Service] IMediator mediator,
    CreateAchievementInput input) {
    var command = new CreateAchievementCommand {
      Name = input.Name,
      Description = input.Description,
      Category = input.Category,
      Type = input.Type,
      IconUrl = input.IconUrl,
      Color = input.Color,
      Points = input.Points,
      IsActive = input.IsActive,
      IsSecret = input.IsSecret,
      IsRepeatable = input.IsRepeatable,
      Conditions = input.Conditions,
      DisplayOrder = input.DisplayOrder,
      TenantId = input.TenantId,
      Levels = input.Levels?.Select(l => new CreateAchievementLevelCommand {
        Level = l.Level,
        Name = l.Name,
        Description = l.Description,
        RequiredProgress = l.RequiredProgress,
        Points = l.Points,
        IconUrl = l.IconUrl,
        Color = l.Color,
      }).ToList(),
      PrerequisiteAchievementIds = input.PrerequisiteAchievementIds,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Update an existing achievement
  /// </summary>
  [Authorize(Roles = new[] { "Admin", "Moderator" })]
  public async Task<Achievement> UpdateAchievement(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    UpdateAchievementInput input) {
    var command = new UpdateAchievementCommand {
      AchievementId = input.AchievementId,
      Name = input.Name,
      Description = input.Description,
      Category = input.Category,
      Type = input.Type,
      IconUrl = input.IconUrl,
      Color = input.Color,
      Points = input.Points,
      IsActive = input.IsActive,
      IsSecret = input.IsSecret,
      IsRepeatable = input.IsRepeatable,
      Conditions = input.Conditions,
      DisplayOrder = input.DisplayOrder,
      UserId = userContext.UserId ?? Guid.Empty,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Delete an achievement
  /// </summary>
  [Authorize(Roles = new[] { "Admin" })]
  public async Task<bool> DeleteAchievement(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    Guid achievementId) {
    var command = new DeleteAchievementCommand {
      AchievementId = achievementId,
      UserId = userContext.UserId ?? Guid.Empty,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess;
  }

  /// <summary>
  /// Award an achievement to a user
  /// </summary>
  [Authorize(Roles = new[] { "Admin", "Moderator" })]
  public async Task<UserAchievement> AwardAchievement(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    [Service] ITenantContext tenantContext,
    AwardAchievementInput input) {
    var command = new AwardAchievementCommand {
      UserId = input.UserId,
      AchievementId = input.AchievementId,
      Level = input.Level,
      Progress = input.Progress,
      MaxProgress = input.MaxProgress,
      Context = input.Context,
      NotifyUser = input.NotifyUser,
      TenantId = tenantContext.TenantId,
      AwardedByUserId = userContext.UserId,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Update achievement progress for a user
  /// </summary>
  [Authorize]
  public async Task<AchievementProgress> UpdateAchievementProgress(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    [Service] ITenantContext tenantContext,
    UpdateAchievementProgressInput input) {
    // Only allow users to update their own progress or admins/moderators
    if (input.UserId != userContext.UserId && 
        !userContext.IsInRole("Admin") && 
        !userContext.IsInRole("Moderator")) {
      throw new GraphQLException("Access denied");
    }

    var command = new UpdateAchievementProgressCommand {
      UserId = input.UserId,
      AchievementId = input.AchievementId,
      ProgressIncrement = input.ProgressIncrement,
      Context = input.Context,
      AutoAward = input.AutoAward,
      TenantId = tenantContext.TenantId,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Revoke an achievement from a user
  /// </summary>
  [Authorize(Roles = new[] { "Admin", "Moderator" })]
  public async Task<bool> RevokeAchievement(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    RevokeAchievementInput input) {
    var command = new RevokeAchievementCommand {
      UserAchievementId = input.UserAchievementId,
      Reason = input.Reason,
      RevokedByUserId = userContext.UserId ?? Guid.Empty,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess;
  }

  /// <summary>
  /// Bulk award an achievement to multiple users
  /// </summary>
  [Authorize(Roles = new[] { "Admin" })]
  public async Task<List<UserAchievement>> BulkAwardAchievement(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    [Service] ITenantContext tenantContext,
    BulkAwardAchievementInput input) {
    var command = new BulkAwardAchievementCommand {
      AchievementId = input.AchievementId,
      UserIds = input.UserIds,
      UserCriteria = input.UserCriteria,
      Context = input.Context,
      NotifyUsers = input.NotifyUsers,
      TenantId = tenantContext.TenantId,
      AwardedByUserId = userContext.UserId ?? Guid.Empty,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess ? result.Value : throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Mark user achievement as notified
  /// </summary>
  [Authorize]
  public async Task<bool> MarkAchievementNotified(
    [Service] IMediator mediator,
    [Service] IUserContext userContext,
    Guid userAchievementId) {
    var command = new MarkAchievementNotifiedCommand {
      UserAchievementId = userAchievementId,
      UserId = userContext.UserId ?? Guid.Empty,
    };

    var result = await mediator.Send(command);
    return result.IsSuccess;
  }
}

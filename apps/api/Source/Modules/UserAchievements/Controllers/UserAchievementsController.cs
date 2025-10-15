using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// REST API controller for user achievements
/// </summary>
[ApiController]
[Route("api/users/{userId:guid}/achievements")]
[Authorize]
public class UserAchievementsController : ControllerBase {
  private readonly IMediator _mediator;
  private readonly ILogger<UserAchievementsController> _logger;

  public UserAchievementsController(IMediator mediator, ILogger<UserAchievementsController> logger) {
    _mediator = mediator;
    _logger = logger;
  }

  /// <summary>
  /// Get user's achievements
  /// </summary>
  [HttpGet]
  public async Task<ActionResult<UserAchievementsPageDto>> GetUserAchievements(
    Guid userId,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? category = null,
    [FromQuery] string? type = null,
    [FromQuery] bool? isCompleted = null,
    [FromQuery] DateTime? earnedAfter = null,
    [FromQuery] DateTime? earnedBefore = null,
    [FromQuery] string orderBy = "EarnedAt",
    [FromQuery] bool descending = true
  ) {
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
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Get user's achievement progress
  /// </summary>
  [HttpGet("progress")]
  public async Task<ActionResult<List<AchievementProgressDto>>> GetUserAchievementProgress(
    Guid userId,
    [FromQuery] string? category = null,
    [FromQuery] bool onlyInProgress = false
  ) {
    var query = new GetUserAchievementProgressQuery {
      UserId = userId,
      Category = category,
      OnlyInProgress = onlyInProgress,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Get user's achievement summary
  /// </summary>
  [HttpGet("summary")]
  public async Task<ActionResult<UserAchievementSummaryDto>> GetUserAchievementSummary(
    Guid userId,
    [FromQuery] int recentLimit = 5,
    [FromQuery] int nearCompletionThreshold = 80
  ) {
    var query = new GetUserAchievementSummaryQuery {
      UserId = userId,
      RecentLimit = recentLimit,
      NearCompletionThreshold = nearCompletionThreshold,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Get available achievements for a user
  /// </summary>
  [HttpGet("available")]
  public async Task<ActionResult<AchievementsPageDto>> GetAvailableAchievements(
    Guid userId,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? category = null,
    [FromQuery] bool onlyEligible = false
  ) {
    var query = new GetAvailableAchievementsQuery {
      UserId = userId,
      PageNumber = pageNumber,
      PageSize = pageSize,
      Category = category,
      OnlyEligible = onlyEligible,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Update achievement progress for a user
  /// </summary>
  [HttpPost("{achievementId:guid}/progress")]
  public async Task<ActionResult<AchievementProgress>> UpdateAchievementProgress(
    Guid userId,
    Guid achievementId,
    [FromBody] UpdateAchievementProgressRequest request
  ) {
    // Only allow users to update their own progress or admins/moderators
    if (userId != GetCurrentUserId() && !User.IsInRole("Admin") && !User.IsInRole("Moderator")) {
      return Forbid();
    }

    var command = new UpdateAchievementProgressCommand {
      UserId = userId,
      AchievementId = achievementId,
      ProgressIncrement = request.ProgressIncrement,
      Context = request.Context,
      AutoAward = request.AutoAward,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Check prerequisites for an achievement
  /// </summary>
  [HttpGet("{achievementId:guid}/prerequisites")]
  public async Task<ActionResult<AchievementPrerequisiteCheckDto>> CheckAchievementPrerequisites(
    Guid userId,
    Guid achievementId
  ) {
    var query = new CheckAchievementPrerequisitesQuery {
      UserId = userId,
      AchievementId = achievementId,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Mark achievement as notified
  /// </summary>
  [HttpPost("{userAchievementId:guid}/mark-notified")]
  public async Task<ActionResult> MarkAchievementNotified(Guid userId, Guid userAchievementId) {
    // Only allow users to mark their own achievements as notified
    if (userId != GetCurrentUserId()) {
      return Forbid();
    }

    var command = new MarkAchievementNotifiedCommand {
      UserAchievementId = userAchievementId,
      UserId = userId,
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess ? NoContent() : BadRequest(result.Error);
  }

  /// <summary>
  /// Revoke an achievement from a user
  /// </summary>
  [HttpDelete("{userAchievementId:guid}")]
  [Authorize(Roles = "Admin,Moderator")]
  public async Task<ActionResult> RevokeAchievement(
    Guid userId,
    Guid userAchievementId,
    [FromBody] RevokeAchievementRequest request
  ) {
    var command = new RevokeAchievementCommand {
      UserAchievementId = userAchievementId,
      Reason = request.Reason,
      RevokedByUserId = GetCurrentUserId(),
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess ? NoContent() : BadRequest(result.Error);
  }

  private Guid GetCurrentUserId() {
    // This should be implemented to get the current user ID from the JWT token or user context
    // For now, returning empty GUID as placeholder
    return Guid.Empty;
  }

  private Guid? GetCurrentTenantId() {
    // This should be implemented to get the current tenant ID from the request context
    // For now, returning null as placeholder
    return null;
  }
}

/// <summary>
/// Alternative controller for achievement leaderboard
/// </summary>
[ApiController]
[Route("api/achievements/leaderboard")]
[Authorize]
public class AchievementLeaderboardController : ControllerBase {
  private readonly IMediator _mediator;
  private readonly ILogger<AchievementLeaderboardController> _logger;

  public AchievementLeaderboardController(IMediator mediator, ILogger<AchievementLeaderboardController> logger) {
    _mediator = mediator;
    _logger = logger;
  }

  /// <summary>
  /// Get achievement leaderboard
  /// </summary>
  [HttpGet]
  public async Task<ActionResult<List<UserAchievementSummaryDto>>> GetLeaderboard(
    [FromQuery] string? category = null,
    [FromQuery] int limit = 50,
    [FromQuery] string orderBy = "TotalPoints",
    [FromQuery] DateTime? timeFrame = null
  ) {
    var query = new GetAchievementLeaderboardQuery {
      Category = category,
      Limit = limit,
      OrderBy = orderBy,
      TimeFrame = timeFrame,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  private Guid? GetCurrentTenantId() {
    // This should be implemented to get the current tenant ID from the request context
    // For now, returning null as placeholder
    return null;
  }
}

/// <summary>
/// Request model for updating achievement progress
/// </summary>
public class UpdateAchievementProgressRequest {
  public int ProgressIncrement { get; set; } = 1;
  public string? Context { get; set; }
  public bool AutoAward { get; set; } = true;
}

/// <summary>
/// Request model for revoking achievements
/// </summary>
public class RevokeAchievementRequest {
  public string? Reason { get; set; }
}

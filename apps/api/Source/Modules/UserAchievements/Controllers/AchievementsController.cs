using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// REST API controller for managing achievements
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AchievementsController : ControllerBase {
  private readonly IMediator _mediator;
  private readonly ILogger<AchievementsController> _logger;

  public AchievementsController(IMediator mediator, ILogger<AchievementsController> logger) {
    _mediator = mediator;
    _logger = logger;
  }

  /// <summary>
  /// Get achievements with pagination and filtering
  /// </summary>
  [HttpGet]
  public async Task<ActionResult<AchievementsPageDto>> GetAchievements(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? category = null,
    [FromQuery] string? type = null,
    [FromQuery] bool? isActive = true,
    [FromQuery] bool? isSecret = null,
    [FromQuery] bool includeSecrets = false,
    [FromQuery] string? searchTerm = null,
    [FromQuery] string orderBy = "DisplayOrder",
    [FromQuery] bool descending = false,
    [FromQuery] Guid? tenantId = null
  ) {
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
      TenantId = tenantId ?? GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Get achievement by ID
  /// </summary>
  [HttpGet("{achievementId:guid}")]
  public async Task<ActionResult<Achievement>> GetAchievement(
    Guid achievementId,
    [FromQuery] bool includeLevels = true,
    [FromQuery] bool includePrerequisites = true) {
    var query = new GetAchievementByIdQuery {
      AchievementId = achievementId,
      IncludeLevels = includeLevels,
      IncludePrerequisites = includePrerequisites,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
  }

  /// <summary>
  /// Create a new achievement
  /// </summary>
  [HttpPost]
  [Authorize(Roles = "Admin,Moderator")]
  public async Task<ActionResult<Achievement>> CreateAchievement([FromBody] CreateAchievementCommand command) {
    command.TenantId = GetCurrentTenantId();
    var result = await _mediator.Send(command);

    return result.IsSuccess
      ? CreatedAtAction(nameof(GetAchievement), new { achievementId = result.Value.Id }, result.Value)
      : BadRequest(result.Error);
  }

  /// <summary>
  /// Update an existing achievement
  /// </summary>
  [HttpPut("{achievementId:guid}")]
  [Authorize(Roles = "Admin,Moderator")]
  public async Task<ActionResult<Achievement>> UpdateAchievement(Guid achievementId, [FromBody] UpdateAchievementCommand command) {
    command.AchievementId = achievementId;
    command.UserId = GetCurrentUserId();
    var result = await _mediator.Send(command);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Delete an achievement
  /// </summary>
  [HttpDelete("{achievementId:guid}")]
  [Authorize(Roles = "Admin")]
  public async Task<ActionResult> DeleteAchievement(Guid achievementId) {
    var command = new DeleteAchievementCommand {
      AchievementId = achievementId,
      UserId = GetCurrentUserId(),
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess ? NoContent() : BadRequest(result.Error);
  }

  /// <summary>
  /// Award an achievement to a user
  /// </summary>
  [HttpPost("{achievementId:guid}/award")]
  [Authorize(Roles = "Admin,Moderator")]
  public async Task<ActionResult<UserAchievement>> AwardAchievement(
    Guid achievementId,
    [FromBody] AwardAchievementRequest request) {
    var command = new AwardAchievementCommand {
      UserId = request.UserId,
      AchievementId = achievementId,
      Level = request.Level,
      Progress = request.Progress,
      MaxProgress = request.MaxProgress,
      Context = request.Context,
      NotifyUser = request.NotifyUser,
      TenantId = GetCurrentTenantId(),
      AwardedByUserId = GetCurrentUserId(),
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Bulk award an achievement to multiple users
  /// </summary>
  [HttpPost("{achievementId:guid}/bulk-award")]
  [Authorize(Roles = "Admin")]
  public async Task<ActionResult<List<UserAchievement>>> BulkAwardAchievement(
    Guid achievementId,
    [FromBody] BulkAwardAchievementRequest request) {
    var command = new BulkAwardAchievementCommand {
      AchievementId = achievementId,
      UserIds = request.UserIds,
      UserCriteria = request.UserCriteria,
      Context = request.Context,
      NotifyUsers = request.NotifyUsers,
      TenantId = GetCurrentTenantId(),
      AwardedByUserId = GetCurrentUserId(),
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Get achievement statistics
  /// </summary>
  [HttpGet("{achievementId:guid}/statistics")]
  public async Task<ActionResult<AchievementStatisticsDto>> GetAchievementStatistics(Guid achievementId) {
    var query = new GetAchievementStatisticsQuery {
      AchievementId = achievementId,
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
  }

  /// <summary>
  /// Get overall achievement statistics
  /// </summary>
  [HttpGet("statistics")]
  public async Task<ActionResult<AchievementStatisticsDto>> GetOverallStatistics() {
    var query = new GetAchievementStatisticsQuery {
      TenantId = GetCurrentTenantId(),
    };

    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
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
/// Request model for awarding achievements
/// </summary>
public class AwardAchievementRequest {
  public Guid UserId { get; set; }
  public int? Level { get; set; }
  public int Progress { get; set; } = 1;
  public int MaxProgress { get; set; } = 1;
  public string? Context { get; set; }
  public bool NotifyUser { get; set; } = true;
}

/// <summary>
/// Request model for bulk awarding achievements
/// </summary>
public class BulkAwardAchievementRequest {
  public List<Guid>? UserIds { get; set; }
  public string? UserCriteria { get; set; }
  public string? Context { get; set; }
  public bool NotifyUsers { get; set; } = true;
}

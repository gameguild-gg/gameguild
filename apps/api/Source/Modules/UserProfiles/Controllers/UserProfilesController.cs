using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// REST API controller for managing user profiles using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserProfilesController(IMediator mediator) : ControllerBase {
  /// <summary>
  /// Get all user profiles with optional filtering and pagination
  /// </summary>
  [HttpGet]
  public async Task<ActionResult<IEnumerable<UserProfileResponseDto>>> GetUserProfiles(
    [FromQuery] bool includeDeleted = false,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] string? searchTerm = null,
    [FromQuery] Guid? tenantId = null
  ) {
    var query = new GetAllUserProfilesQuery {
      IncludeDeleted = includeDeleted,
      Skip = skip,
      Take = take,
      SearchTerm = searchTerm,
      TenantId = tenantId,
    };

    var userProfiles = await mediator.Send(query);

    if (!userProfiles.IsSuccess) return BadRequest(userProfiles.ErrorMessage);

    var userProfileDtos = userProfiles.Value.Select(up => new UserProfileResponseDto {
        Id = up.Id,
        Version = up.Version,
        GivenName = up.GivenName,
        FamilyName = up.FamilyName,
        DisplayName = up.DisplayName,
        Title = up.Title,
        Description = up.Description,
        CreatedAt = up.CreatedAt,
        UpdatedAt = up.UpdatedAt,
        DeletedAt = up.DeletedAt,
        IsDeleted = up.IsDeleted,
      }
    );

    return Ok(userProfileDtos);
  }

  /// <summary>
  /// Get user profile by ID
  /// </summary>
  [HttpGet("{id}")]
  public async Task<ActionResult<UserProfileResponseDto>> GetUserProfile(Guid id, [FromQuery] bool includeDeleted = false) {
    var query = new GetUserProfileByIdQuery { UserProfileId = id, IncludeDeleted = includeDeleted, };

    var userProfile = await mediator.Send(query);

    if (!userProfile.IsSuccess) return BadRequest(userProfile.ErrorMessage);

    if (userProfile.Value == null) return PageNotFound();

    var userProfileDto = new UserProfileResponseDto {
      Id = userProfile.Value.Id,
      Version = userProfile.Value.Version,
      GivenName = userProfile.Value.GivenName,
      FamilyName = userProfile.Value.FamilyName,
      DisplayName = userProfile.Value.DisplayName,
      Title = userProfile.Value.Title,
      Description = userProfile.Value.Description,
      CreatedAt = userProfile.Value.CreatedAt,
      UpdatedAt = userProfile.Value.UpdatedAt,
      DeletedAt = userProfile.Value.DeletedAt,
      IsDeleted = userProfile.Value.IsDeleted,
    };

    return Ok(userProfileDto);
  }

  /// <summary>
  /// Get user profile by user ID
  /// </summary>
  [HttpGet("user/{userId}")]
  public async Task<ActionResult<UserProfileResponseDto>> GetUserProfileByUserId(Guid userId, [FromQuery] bool includeDeleted = false) {
    var query = new GetUserProfileByUserIdQuery { UserId = userId, IncludeDeleted = includeDeleted, };

    var userProfile = await mediator.Send(query);

    if (!userProfile.IsSuccess) return BadRequest(userProfile.ErrorMessage);

    if (userProfile.Value == null) return PageNotFound();

    var userProfileDto = new UserProfileResponseDto {
      Id = userProfile.Value.Id,
      Version = userProfile.Value.Version,
      GivenName = userProfile.Value.GivenName,
      FamilyName = userProfile.Value.FamilyName,
      DisplayName = userProfile.Value.DisplayName,
      Title = userProfile.Value.Title,
      Description = userProfile.Value.Description,
      CreatedAt = userProfile.Value.CreatedAt,
      UpdatedAt = userProfile.Value.UpdatedAt,
      DeletedAt = userProfile.Value.DeletedAt,
      IsDeleted = userProfile.Value.IsDeleted,
    };

    return Ok(userProfileDto);
  }

  /// <summary>
  /// Create a new user profile
  /// </summary>
  [HttpPost]
  public async Task<ActionResult<UserProfileResponseDto>> CreateUserProfile([FromBody] CreateUserProfileDto createDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var command = new CreateUserProfileCommand {
      GivenName = createDto.GivenName ?? string.Empty,
      FamilyName = createDto.FamilyName ?? string.Empty,
      DisplayName = createDto.DisplayName ?? string.Empty,
      Title = createDto.Title,
      Description = createDto.Description,
      UserId = createDto.UserId ?? Guid.NewGuid(), // Should be provided in DTO
      TenantId = createDto.TenantId,
    };

    var userProfile = await mediator.Send(command);

    if (!userProfile.IsSuccess) return BadRequest(userProfile.ErrorMessage);

    var userProfileDto = new UserProfileResponseDto {
      Id = userProfile.Value.Id,
      Version = userProfile.Value.Version,
      GivenName = userProfile.Value.GivenName,
      FamilyName = userProfile.Value.FamilyName,
      DisplayName = userProfile.Value.DisplayName,
      Title = userProfile.Value.Title,
      Description = userProfile.Value.Description,
      CreatedAt = userProfile.Value.CreatedAt,
      UpdatedAt = userProfile.Value.UpdatedAt,
      DeletedAt = userProfile.Value.DeletedAt,
      IsDeleted = userProfile.Value.IsDeleted,
    };

    return CreatedAtAction(nameof(GetUserProfile), new { id = userProfile.Value.Id }, userProfileDto);
  }

  /// <summary>
  /// Update a user profile
  /// </summary>
  [HttpPut("{id}")]
  public async Task<ActionResult<UserProfileResponseDto>> UpdateUserProfile(
    Guid id,
    [FromBody] UpdateUserProfileDto updateDto,
    [FromHeader(Name = "If-Match")] int? ifMatch = null
  ) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var command = new UpdateUserProfileCommand {
      UserProfileId = id,
      GivenName = updateDto.GivenName,
      FamilyName = updateDto.FamilyName,
      DisplayName = updateDto.DisplayName,
      Title = updateDto.Title,
      Description = updateDto.Description,
      ExpectedVersion = ifMatch,
    };

    try {
      var userProfile = await mediator.Send(command);

      if (!userProfile.IsSuccess) return BadRequest(userProfile.ErrorMessage);

      var userProfileDto = new UserProfileResponseDto {
        Id = userProfile.Value.Id,
        Version = userProfile.Value.Version,
        GivenName = userProfile.Value.GivenName,
        FamilyName = userProfile.Value.FamilyName,
        DisplayName = userProfile.Value.DisplayName,
        Title = userProfile.Value.Title,
        Description = userProfile.Value.Description,
        CreatedAt = userProfile.Value.CreatedAt,
        UpdatedAt = userProfile.Value.UpdatedAt,
        DeletedAt = userProfile.Value.DeletedAt,
        IsDeleted = userProfile.Value.IsDeleted,
      };

      return Ok(userProfileDto);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict")) { return Conflict(new { message = ex.Message }); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found")) { return PageNotFound(new { message = ex.Message }); }
  }

  /// <summary>
  /// Delete a user profile (soft delete by default)
  /// </summary>
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteUserProfile(Guid id, [FromQuery] bool permanent = false) {
    var command = new DeleteUserProfileCommand { UserProfileId = id, SoftDelete = !permanent, };

    var result = await mediator.Send(command);

    if (!result.IsSuccess) return BadRequest(result.ErrorMessage);

    if (!result.Value) return PageNotFound();

    return NoContent();
  }

  /// <summary>
  /// Restore a soft-deleted user profile
  /// </summary>
  [HttpPost("{id}/restore")]
  public async Task<IActionResult> RestoreUserProfile(Guid id) {
    var command = new RestoreUserProfileCommand { UserProfileId = id, };

    var result = await mediator.Send(command);

    if (!result.IsSuccess) return BadRequest(result.ErrorMessage);

    if (!result.Value) return PageNotFound();

    return NoContent();
  }
}

using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.UserProfiles;

/// <summary> REST API controller for managing user profiles using CQRS pattern </summary>
[ApiController]
[Route("[controller]")]
public class UserProfilesController(IMediator mediator) : ControllerBase
{
    /// <summary> Get all user profiles with optional filtering and pagination </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetUserProfiles(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? tenantId = null
    )
    {
        var query = new GetAllUserProfilesQuery { IncludeDeleted = includeDeleted, Skip = skip, Take = take, SearchTerm = searchTerm, TenantId = tenantId };

        var userProfiles = await mediator.Send(query);

        if (!userProfiles.IsSuccess) return BadRequest(userProfiles.Error);

        var userProfileDtos = userProfiles.Value.Select(up => new UserProfileResponse
            {
                Id = up.Id, Version = up.Version, DisplayName = up.DisplayName, TenantId = up.Tenant?.Id, CreatedAt = up.CreatedAt, UpdatedAt = up.UpdatedAt, DeletedAt = up.DeletedAt, IsDeleted = up.IsDeleted,
            }
        );

        return Ok(userProfileDtos);
    }

    /// <summary> Get user profile by ID </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfileResponse>> GetUserProfile(Guid id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetUserProfileByIdQuery { UserProfileId = id, IncludeDeleted = includeDeleted };

        var userProfile = await mediator.Send(query);

        if (!userProfile.IsSuccess) return BadRequest(userProfile.Error);

        if (userProfile.Value == null) return NotFound();

        var userProfileDto = new UserProfileResponse
        {
            Id = userProfile.Value.Id,
            Version = userProfile.Value.Version,
            DisplayName = userProfile.Value.DisplayName,
            TenantId = userProfile.Value.Tenant?.Id,
            CreatedAt = userProfile.Value.CreatedAt,
            UpdatedAt = userProfile.Value.UpdatedAt,
            DeletedAt = userProfile.Value.DeletedAt,
            IsDeleted = userProfile.Value.IsDeleted,
        };

        return Ok(userProfileDto);
    }

    /// <summary> Get user profile by user ID </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<UserProfileResponse>> GetUserProfileByUserId(Guid userId, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetUserProfileByUserIdQuery { UserId = userId, IncludeDeleted = includeDeleted };

        var userProfile = await mediator.Send(query);

        if (!userProfile.IsSuccess) return BadRequest(userProfile.Error);

        if (userProfile.Value == null) return NotFound();

        var userProfileDto = new UserProfileResponse
        {
            Id = userProfile.Value.Id,
            Version = userProfile.Value.Version,
            DisplayName = userProfile.Value.DisplayName,
            TenantId = userProfile.Value.Tenant?.Id,
            CreatedAt = userProfile.Value.CreatedAt,
            UpdatedAt = userProfile.Value.UpdatedAt,
            DeletedAt = userProfile.Value.DeletedAt,
            IsDeleted = userProfile.Value.IsDeleted,
        };

        return Ok(userProfileDto);
    }

    /// <summary> Create a new user profile </summary>
    [HttpPost]
    public async Task<ActionResult<UserProfileResponse>> CreateUserProfile([FromBody] CreateUserProfileRequest createRequest)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var command = new CreateUserProfileCommand
        {
            DisplayName = createRequest.DisplayName,
            UserId = createRequest.UserId ?? Guid.NewGuid(), // Should be provided in DTO
            TenantId = createRequest.TenantId,
        };

        var userProfile = await mediator.Send(command);

        if (!userProfile.IsSuccess) return BadRequest(userProfile.Error);

        var userProfileDto = new UserProfileResponse
        {
            Id = userProfile.Value.Id,
            Version = userProfile.Value.Version,
            DisplayName = userProfile.Value.DisplayName,
            TenantId = userProfile.Value.Tenant?.Id,
            CreatedAt = userProfile.Value.CreatedAt,
            UpdatedAt = userProfile.Value.UpdatedAt,
            DeletedAt = userProfile.Value.DeletedAt,
            IsDeleted = userProfile.Value.IsDeleted,
        };

        return CreatedAtAction(nameof(GetUserProfile), new { id = userProfile.Value.Id }, userProfileDto);
    }

    /// <summary> Update a user profile </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserProfileResponse>> UpdateUserProfile(Guid id, [FromBody] UpdateUserProfileRequest updateRequest, [FromHeader(Name = "If-Match")] int? ifMatch = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var command = new UpdateUserProfileCommand { UserProfileId = id, DisplayName = updateRequest.DisplayName, ExpectedVersion = ifMatch, };

        try
        {
            var userProfile = await mediator.Send(command);

            if (!userProfile.IsSuccess) return BadRequest(userProfile.Error);

            var userProfileDto = new UserProfileResponse
            {
                Id = userProfile.Value.Id,
                Version = userProfile.Value.Version,
                DisplayName = userProfile.Value.DisplayName,
                TenantId = userProfile.Value.Tenant?.Id,
                CreatedAt = userProfile.Value.CreatedAt,
                UpdatedAt = userProfile.Value.UpdatedAt,
                DeletedAt = userProfile.Value.DeletedAt,
                IsDeleted = userProfile.Value.IsDeleted,
            };

            return Ok(userProfileDto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict")) { return Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found")) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary> Delete a user profile (soft delete by default) </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserProfile(Guid id, [FromQuery] bool permanent = false)
    {
        var command = new DeleteUserProfileCommand { UserProfileId = id, SoftDelete = !permanent };

        var result = await mediator.Send(command);

        if (!result.IsSuccess) return BadRequest(result.Error);

        if (!result.Value) return NotFound();

        return NoContent();
    }

    /// <summary> Restore a soft-deleted user profile </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreUserProfile(Guid id)
    {
        var command = new RestoreUserProfileCommand { UserProfileId = id };

        var result = await mediator.Send(command);

        if (!result.IsSuccess) return BadRequest(result.Error);

        if (!result.Value) return NotFound();

        return NoContent();
    }
}

using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Users;

/// <summary> REST API controller for managing users using CQRS pattern </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class UsersController(IMediator mediator, ILogger<UsersController> logger) : ControllerBase
{
    /// <summary> Get all users with optional filtering and pagination </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers([FromQuery] bool includeDeleted = false, [FromQuery] int skip = 0, [FromQuery] int take = 50, [FromQuery] bool? isActive = null)
    {
        logger.LogDebug("Getting users with includeDeleted={IncludeDeleted}, skip={Skip}, take={Take}, isActive={IsActive}", includeDeleted, skip, take, isActive);
        var query = new GetAllUsersQuery { IncludeDeleted = includeDeleted, Skip = skip, Take = take, IsActive = isActive };

        var users = await mediator.Send(query);

        var userDtos = users.Select(u => new UserResponse
                {
                    Id = u.Id,
                    Version = u.Version,
                    Name = u.Name,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    DeletedAt = u.DeletedAt,
                    IsDeleted = u.DeletedAt != null,
                }
            )
            .ToList();

        return Ok(userDtos);
    }

    /// <summary> Get a user by ID </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id, [FromQuery] bool includeDeleted = false)
    {
        logger.LogDebug("Getting user with ID {UserId}", id);
        var query = new GetUserByIdQuery { UserId = id, IncludeDeleted = includeDeleted };

        var user = await mediator.Send(query);

        if (user == null) return NotFound($"User with ID {id} not found");

        var userDto = new UserResponse
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null,
        };

        return Ok(userDto);
    }

    /// <summary> Create a new user </summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest createRequest)
    {
        logger.LogDebug("Creating user with email {Email}", createRequest.Email);
        var command = new CreateUserCommand { Name = createRequest.Name, Email = createRequest.Email, IsActive = createRequest.IsActive };

        var user = await mediator.Send(command);

        logger.LogInformation("Successfully created user {UserId} with email {Email}", user.Id, user.Email);

        var userDto = new UserResponse
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null,
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    /// <summary> Update a user </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, [FromBody] UpdateUserRequest updateRequest)
    {
        var command = new UpdateUserCommand
        {
            UserId = id, Name = updateRequest.Name, Username = updateRequest.Username, Email = updateRequest.Email, IsActive = updateRequest.IsActive, ExpectedVersion = updateRequest.ExpectedVersion
        };

        var user = await mediator.Send(command);

        var userDto = new UserResponse
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null,
        };

        return Ok(userDto);
    }

    /// <summary> Delete a user </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] bool softDelete = true, [FromQuery] string? reason = null)
    {
        logger.LogDebug("Deleting user with ID {UserId}, softDelete={SoftDelete}", id, softDelete);
        var command = new DeleteUserCommand { UserId = id, SoftDelete = softDelete, Reason = reason };

        var result = await mediator.Send(command);

        if (!result) return NotFound($"User with ID {id} not found or already deleted");

        return NoContent();
    }

    /// <summary> Restore a soft-deleted user </summary>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreUser(Guid id, [FromQuery] string? reason = null)
    {
        var command = new RestoreUserCommand { UserId = id, Reason = reason };

        var result = await mediator.Send(command);

        if (!result) return NotFound($"User with ID {id} not found or not deleted");

        return NoContent();
    }

    /// <summary> Bulk create multiple users </summary>
    [HttpPost("bulk/create")]
    public async Task<ActionResult<BulkOperationResult>> BulkCreateUsers([FromBody] List<CreateUserRequest> users, [FromQuery] string? reason = null)
    {
        var command = new BulkCreateUsersCommand { Users = users, Reason = reason };
        var result = await mediator.Send(command);

        return Ok(result);
    }

    /// <summary> Bulk activate multiple users </summary>
    [HttpPatch("bulk/activate")]
    public async Task<ActionResult<BulkOperationResult>> BulkActivateUsers([FromBody] List<Guid> userIds, [FromQuery] string? reason = null)
    {
        var command = new BulkActivateUsersCommand { UserIds = userIds, Reason = reason };
        var result = await mediator.Send(command);

        return Ok(result);
    }

    /// <summary> Bulk deactivate multiple users </summary>
    [HttpPatch("bulk/deactivate")]
    public async Task<ActionResult<BulkOperationResult>> BulkDeactivateUsers([FromBody] List<Guid> userIds, [FromQuery] string? reason = null)
    {
        var command = new BulkDeactivateUsersCommand { UserIds = userIds, Reason = reason };
        var result = await mediator.Send(command);

        return Ok(result);
    }

    /// <summary> Get user statistics </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<UserStatistics>> GetUserStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetUserStatisticsQuery { FromDate = fromDate, ToDate = toDate, IncludeDeleted = includeDeleted };

        var statistics = await mediator.Send(query);

        return Ok(statistics);
    }
}

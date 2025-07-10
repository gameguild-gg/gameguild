using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Users.Controllers;

/// <summary>
/// REST API controller for managing users using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all users with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetAllUsersQuery
        {
            IncludeDeleted = includeDeleted,
            Skip = skip,
            Take = take,
            IsActive = isActive
        };

        var users = await mediator.Send(query);

        var userDtos = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Version = u.Version,
            Name = u.Name,
            Email = u.Email,
            IsActive = u.IsActive,
            Balance = u.Balance,
            AvailableBalance = u.AvailableBalance,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            DeletedAt = u.DeletedAt,
            IsDeleted = u.DeletedAt != null
        }).ToList();

        return Ok(userDtos);
    }

    /// <summary>
    /// Get a user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(Guid id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetUserByIdQuery
        {
            UserId = id,
            IncludeDeleted = includeDeleted
        };

        var user = await mediator.Send(query);

        if (user == null)
            return NotFound($"User with ID {id} not found");

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            Balance = user.Balance,
            AvailableBalance = user.AvailableBalance,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createDto)
    {
        var command = new CreateUserCommand
        {
            Name = createDto.Name,
            Email = createDto.Email,
            IsActive = createDto.IsActive,
            InitialBalance = createDto.InitialBalance
        };

        var user = await mediator.Send(command);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            Balance = user.Balance,
            AvailableBalance = user.AvailableBalance,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateDto)
    {
        var command = new UpdateUserCommand
        {
            UserId = id,
            Name = updateDto.Name,
            Email = updateDto.Email,
            IsActive = updateDto.IsActive
        };

        var user = await mediator.Send(command);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            Balance = user.Balance,
            AvailableBalance = user.AvailableBalance,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] bool softDelete = true)
    {
        var command = new DeleteUserCommand
        {
            UserId = id,
            SoftDelete = softDelete
        };

        var result = await mediator.Send(command);

        if (!result)
            return NotFound($"User with ID {id} not found or already deleted");

        return NoContent();
    }

    /// <summary>
    /// Restore a soft-deleted user
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreUser(Guid id)
    {
        var command = new RestoreUserCommand
        {
            UserId = id
        };

        var result = await mediator.Send(command);

        if (!result)
            return NotFound($"User with ID {id} not found or not deleted");

        return NoContent();
    }

    /// <summary>
    /// Update user balance
    /// </summary>
    [HttpPut("{id:guid}/balance")]
    public async Task<ActionResult<UserResponseDto>> UpdateUserBalance(Guid id, [FromBody] UpdateUserBalanceCommand updateCommand)
    {
        updateCommand.UserId = id;
        var user = await mediator.Send(updateCommand);

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            Version = user.Version,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            Balance = user.Balance,
            AvailableBalance = user.AvailableBalance,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt,
            IsDeleted = user.DeletedAt != null
        };

        return Ok(userDto);
    }
}

using GameGuild.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Users;

/// <summary>
/// REST API controller for managing users using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase {
  /// <summary>
  /// Get all users with optional filtering and pagination
  /// </summary>
  [HttpGet]
  public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers(
    [FromQuery] bool includeDeleted = false,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] bool? isActive = null
  ) {
    var query = new GetAllUsersQuery { IncludeDeleted = includeDeleted, Skip = skip, Take = take, IsActive = isActive };

    var users = await mediator.Send(query);

    var userDtos = users.Select(u => new UserResponseDto {
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
                            IsDeleted = u.DeletedAt != null,
                          }
                        )
                        .ToList();

    return Ok(userDtos);
  }

  /// <summary>
  /// Get a user by ID
  /// </summary>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<UserResponseDto>> GetUser(Guid id, [FromQuery] bool includeDeleted = false) {
    var query = new GetUserByIdQuery { UserId = id, IncludeDeleted = includeDeleted };

    var user = await mediator.Send(query);

    if (user == null) return NotFound($"User with ID {id} not found");

    var userDto = new UserResponseDto {
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
      IsDeleted = user.DeletedAt != null,
    };

    return Ok(userDto);
  }

  /// <summary>
  /// Create a new user
  /// </summary>
  [HttpPost]
  public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createDto) {
    var command = new CreateUserCommand { Name = createDto.Name, Email = createDto.Email, IsActive = createDto.IsActive, InitialBalance = createDto.InitialBalance };

    var user = await mediator.Send(command);

    var userDto = new UserResponseDto {
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
      IsDeleted = user.DeletedAt != null,
    };

    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
  }

  /// <summary>
  /// Update a user
  /// </summary>
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateDto) {
    var command = new UpdateUserCommand {
      UserId = id,
      Name = updateDto.Name,
      Email = updateDto.Email,
      IsActive = updateDto.IsActive,
      ExpectedVersion = updateDto.ExpectedVersion,
    };

    var user = await mediator.Send(command);

    var userDto = new UserResponseDto {
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
      IsDeleted = user.DeletedAt != null,
    };

    return Ok(userDto);
  }

  /// <summary>
  /// Delete a user
  /// </summary>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] bool softDelete = true, [FromQuery] string? reason = null) {
    var command = new DeleteUserCommand { UserId = id, SoftDelete = softDelete, Reason = reason };

    var result = await mediator.Send(command);

    if (!result) return NotFound($"User with ID {id} not found or already deleted");

    return NoContent();
  }

  /// <summary>
  /// Restore a soft-deleted user
  /// </summary>
  [HttpPost("{id:guid}/restore")]
  public async Task<IActionResult> RestoreUser(Guid id, [FromQuery] string? reason = null) {
    var command = new RestoreUserCommand { UserId = id, Reason = reason };

    var result = await mediator.Send(command);

    if (!result) return NotFound($"User with ID {id} not found or not deleted");

    return NoContent();
  }

  /// <summary>
  /// Update user balance
  /// </summary>
  [HttpPut("{id:guid}/balance")]
  public async Task<ActionResult<UserResponseDto>> UpdateUserBalance(Guid id, [FromBody] UpdateUserBalanceDto updateDto) {
    var command = new UpdateUserBalanceCommand {
      UserId = id,
      Balance = updateDto.Balance,
      AvailableBalance = updateDto.AvailableBalance,
      Reason = updateDto.Reason,
      ExpectedVersion = updateDto.ExpectedVersion,
    };

    var user = await mediator.Send(command);

    var userDto = new UserResponseDto {
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
      IsDeleted = user.DeletedAt != null,
    };

    return Ok(userDto);
  }

  /// <summary>
  /// Get user statistics
  /// </summary>
  [HttpGet("statistics")]
  public async Task<ActionResult<UserStatistics>> GetUserStatistics(
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    [FromQuery] bool includeDeleted = false
  ) {
    var query = new GetUserStatisticsQuery { FromDate = fromDate, ToDate = toDate, IncludeDeleted = includeDeleted };

    var statistics = await mediator.Send(query);

    return Ok(statistics);
  }

  /// <summary>
  /// Search users
  /// </summary>
  [HttpGet("search")]
  public async Task<ActionResult<PagedResult<UserResponseDto>>> SearchUsers(
    [FromQuery] string? searchTerm = null,
    [FromQuery] bool? isActive = null,
    [FromQuery] decimal? minBalance = null,
    [FromQuery] decimal? maxBalance = null,
    [FromQuery] DateTime? createdAfter = null,
    [FromQuery] DateTime? createdBefore = null,
    [FromQuery] bool includeDeleted = false,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] UserSortField sortBy = UserSortField.UpdatedAt,
    [FromQuery] SortDirection sortDirection = SortDirection.Descending
  ) {
    var query = new SearchUsersQuery {
      SearchTerm = searchTerm,
      IsActive = isActive,
      MinBalance = minBalance,
      MaxBalance = maxBalance,
      CreatedAfter = createdAfter,
      CreatedBefore = createdBefore,
      IncludeDeleted = includeDeleted,
      Skip = skip,
      Take = take,
      SortBy = sortBy,
      SortDirection = sortDirection,
    };

    var users = await mediator.Send(query);

    var userDtos = users.Items.Select(u => new UserResponseDto {
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
                            IsDeleted = u.DeletedAt != null,
                          }
                        )
                        .ToList();

    return Ok(new PagedResult<UserResponseDto>(userDtos, users.TotalCount, users.Skip, users.Take));
  }

  /// <summary>
  /// Bulk create multiple users
  /// </summary>
  [HttpPost("bulk")]
  public async Task<ActionResult<BulkOperationResult>> BulkCreateUsers([FromBody] List<CreateUserDto> users, [FromQuery] string? reason = null) {
    var command = new BulkCreateUsersCommand { Users = users, Reason = reason };
    var result = await mediator.Send(command);
    return Ok(result);
  }

  /// <summary>
  /// Bulk activate multiple users
  /// </summary>
  [HttpPatch("bulk/activate")]
  public async Task<ActionResult<BulkOperationResult>> BulkActivateUsers([FromBody] List<Guid> userIds, [FromQuery] string? reason = null) {
    var command = new BulkActivateUsersCommand { UserIds = userIds, Reason = reason };
    var result = await mediator.Send(command);
    return Ok(result);
  }

  /// <summary>
  /// Bulk deactivate multiple users
  /// </summary>
  [HttpPatch("bulk/deactivate")]
  public async Task<ActionResult<BulkOperationResult>> BulkDeactivateUsers([FromBody] List<Guid> userIds, [FromQuery] string? reason = null) {
    var command = new BulkDeactivateUsersCommand { UserIds = userIds, Reason = reason };
    var result = await mediator.Send(command);
    return Ok(result);
  }
}

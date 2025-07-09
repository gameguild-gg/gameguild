using GameGuild.Modules.Users.Dtos;
using GameGuild.Modules.Users.Services;
using GameGuild.Modules.Subscriptions.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Users.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(IUserService userService, ISubscriptionService subscriptionService) : ControllerBase {
  // GET: users

  [HttpGet]
  public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers() {
    var users = await userService.GetAllUsersAsync();

    var userDtos = users.Select(u => new UserResponseDto {
        Id = u.Id,
        Version = u.Version,
        Name = u.Name,
        Email = u.Email,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
        DeletedAt = u.DeletedAt,
        IsDeleted = u.IsDeleted,
      }
    );

    return Ok(userDtos);
  }

  // GET: users/deleted
  [HttpGet("deleted")]
  public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetDeletedUsers() {
    var users = await userService.GetDeletedUsersAsync();

    var userDtos = users.Select(u => new UserResponseDto {
        Id = u.Id,
        Version = u.Version,
        Name = u.Name,
        Email = u.Email,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
        DeletedAt = u.DeletedAt,
        IsDeleted = u.IsDeleted,
      }
    );

    return Ok(userDtos);
  }

  // GET: users/{id}
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<UserResponseDto>> GetUser(Guid id) {
    var user = await userService.GetUserByIdAsync(id);

    if (user == null) return NotFound();

    var userDto = new UserResponseDto {
      Id = user.Id,
      Version = user.Version,
      Name = user.Name,
      Email = user.Email,
      IsActive = user.IsActive,
      CreatedAt = user.CreatedAt,
      UpdatedAt = user.UpdatedAt,
      DeletedAt = user.DeletedAt,
      IsDeleted = user.IsDeleted,
    };

    return Ok(userDto);
  }

  // POST: users
  [HttpPost]
  public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto createUserDto) {
    // Use BaseEntity.Create for consistent creation pattern
    var user = new Models.User(new { createUserDto.Name, createUserDto.Email, IsActive = true });

    var createdUser = await userService.CreateUserAsync(user);

    var userDto = new UserResponseDto {
      Id = createdUser.Id,
      Version = createdUser.Version,
      Name = createdUser.Name,
      Email = createdUser.Email,
      IsActive = createdUser.IsActive,
      CreatedAt = createdUser.CreatedAt,
      UpdatedAt = createdUser.UpdatedAt,
      DeletedAt = createdUser.DeletedAt,
      IsDeleted = createdUser.IsDeleted,
    };

    return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, userDto);
  }

  // PUT: users/{id}
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, UpdateUserDto updateUserDto) {
    var existingUser = await userService.GetUserByIdAsync(id);

    if (existingUser == null) return NotFound();

    // Update only provided properties
    if (!string.IsNullOrEmpty(updateUserDto.Name)) existingUser.Name = updateUserDto.Name;

    if (!string.IsNullOrEmpty(updateUserDto.Email)) existingUser.Email = updateUserDto.Email;

    var updatedUser = await userService.UpdateUserAsync(id, existingUser);

    if (updatedUser == null) return NotFound();

    var userDto = new UserResponseDto {
      Id = updatedUser.Id,
      Version = updatedUser.Version,
      Name = updatedUser.Name,
      Email = updatedUser.Email,
      IsActive = updatedUser.IsActive,
      CreatedAt = updatedUser.CreatedAt,
      UpdatedAt = updatedUser.UpdatedAt,
      DeletedAt = updatedUser.DeletedAt,
      IsDeleted = updatedUser.IsDeleted,
    };

    return Ok(userDto);
  }

  // DELETE: users/{id}
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> DeleteUser(Guid id) {
    var result = await userService.DeleteUserAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  // DELETE: users/{id}/soft
  [HttpDelete("{id:guid}/soft")]
  public async Task<IActionResult> SoftDeleteUser(Guid id) {
    var result = await userService.SoftDeleteUserAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  // POST: users/{id}/restore
  [HttpPost("{id:guid}/restore")]
  public async Task<IActionResult> RestoreUser(Guid id) {
    var result = await userService.RestoreUserAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  // GET: users/me
  [HttpGet("me")]
  public async Task<ActionResult<UserResponseDto>> GetCurrentUser() {
    // Extract user ID from JWT token claims
    var userIdClaim = User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim)) { return Unauthorized(new { message = "User ID not found in token" }); }

    if (!Guid.TryParse(userIdClaim, out var userId)) { return BadRequest(new { message = "Invalid user ID format" }); }

    var user = await userService.GetUserByIdAsync(userId);

    if (user == null) { return NotFound(new { message = "User not found" }); }

    // Get user's active subscription
    var activeSubscription = await subscriptionService.GetActiveSubscriptionAsync(userId);

    var userDto = new UserResponseDto {
      Id = user.Id,
      Version = user.Version,
      Name = user.Name,
      Email = user.Email,
      IsActive = user.IsActive,
      CreatedAt = user.CreatedAt,
      UpdatedAt = user.UpdatedAt,
      DeletedAt = user.DeletedAt,
      IsDeleted = user.IsDeleted,
      Role = "Game Developer", // You can enhance this with actual role logic
      SubscriptionType = activeSubscription?.Status.ToString() ?? "Free Trial",
      ActiveSubscription = activeSubscription != null ? new UserSubscriptionSummaryDto {
        Id = activeSubscription.Id,
        Status = activeSubscription.Status,
        PlanName = "Premium Plan", // You'll need to get this from the subscription plan
        CurrentPeriodStart = activeSubscription.CurrentPeriodStart,
        CurrentPeriodEnd = activeSubscription.CurrentPeriodEnd,
        TrialEndsAt = activeSubscription.TrialEndsAt,
        NextBillingAt = activeSubscription.NextBillingAt,
        IsTrialActive = activeSubscription.TrialEndsAt.HasValue && activeSubscription.TrialEndsAt.Value > DateTime.UtcNow,
        IsActive = activeSubscription.Status == Common.Enums.SubscriptionStatus.Active && activeSubscription.CurrentPeriodEnd > DateTime.UtcNow
      } : null
    };

    return Ok(userDto);
  }
}

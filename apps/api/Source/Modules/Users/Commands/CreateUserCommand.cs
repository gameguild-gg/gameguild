using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameGuild.Common;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to create a new user with validation and authorization
/// </summary>
public class CreateUserCommand : IRequest<User>, IAuthorizedRequest {
  [Required]
  [StringLength(100, MinimumLength = 1)]
  public string Name { get; init; } = string.Empty;

  [Required]
  [EmailAddress]
  [StringLength(255)]
  public string Email { get; init; } = string.Empty;

  public bool IsActive { get; init; } = true;

  [Range(0, double.MaxValue)] public decimal InitialBalance { get; init; }

  // Authorization implementation
  public string[]? RequiredRoles { get; } = ["Admin", "UserManager"];

  public string[]? RequiredPermissions { get; } = ["users.create"];

  public Task<bool> IsAuthorizedAsync(ClaimsPrincipal? user, CancellationToken cancellationToken) {
    // Custom authorization logic - example: check if user can create users in their tenant
    return Task.FromResult(user?.FindFirst("tenant_id") != null);
    // Add custom business logic here
  }
}

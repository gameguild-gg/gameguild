using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameGuild.Common.Behaviors;
using MediatR;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
/// Command to create a new user with validation and authorization
/// </summary>
public class CreateUserCommand : IRequest<Models.User>, IAuthorizedRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; } = 0;

    // Authorization implementation
    public string[]? RequiredRoles => new[] { "Admin", "UserManager" };
    public string[]? RequiredPermissions => new[] { "users.create" };

    public Task<bool> IsAuthorizedAsync(ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        // Custom authorization logic - example: check if user can create users in their tenant
        if (user?.FindFirst("tenant_id") != null)
        {
            // Add custom business logic here
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
}

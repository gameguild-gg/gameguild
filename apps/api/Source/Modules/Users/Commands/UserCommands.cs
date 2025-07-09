using System.ComponentModel.DataAnnotations;
using MediatR;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
/// Command to update user information with validation and business logic
/// </summary>
public class UpdateUserCommand : IRequest<Models.User>
{
    [Required]
    public Guid UserId { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// Expected version for optimistic concurrency control
    /// </summary>
    public int? ExpectedVersion { get; set; }
}

/// <summary>
/// Command to delete a user
/// </summary>
public class DeleteUserCommand : IRequest<bool>
{
    [Required]
    public Guid UserId { get; set; }

    public bool SoftDelete { get; set; } = true;
}

/// <summary>
/// Command to restore a soft-deleted user
/// </summary>
public class RestoreUserCommand : IRequest<bool>
{
    [Required]
    public Guid UserId { get; set; }
}

/// <summary>
/// Command to update user balance
/// </summary>
public class UpdateUserBalanceCommand : IRequest<Models.User>
{
    [Required]
    public Guid UserId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AvailableBalance { get; set; }

    public string? Reason { get; set; }
}

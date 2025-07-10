using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Models;
using MediatR;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
/// Command to update user information with validation and business logic
/// </summary>
public sealed class UpdateUserCommand : IRequest<User>
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
public sealed class DeleteUserCommand : IRequest<bool>
{
    [Required]
    public Guid UserId { get; set; }

    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// Reason for deletion (for audit purposes)
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Command to restore a soft-deleted user
/// </summary>
public sealed class RestoreUserCommand : IRequest<bool>
{
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Reason for restoration (for audit purposes)
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Command to update user balance
/// </summary>
public sealed class UpdateUserBalanceCommand : IRequest<User>
{
    [Required]
    public Guid UserId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AvailableBalance { get; set; }

    public string? Reason { get; set; }

    /// <summary>
    /// Expected version for optimistic concurrency control
    /// </summary>
    public int? ExpectedVersion { get; set; }
}

/// <summary>
/// Command to activate a user
/// </summary>
public sealed class ActivateUserCommand : IRequest<bool>
{
    [Required]
    public Guid UserId { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// Command to deactivate a user
/// </summary>
public sealed class DeactivateUserCommand : IRequest<bool>
{
    [Required]
    public Guid UserId { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// Command to bulk delete users
/// </summary>
public sealed class BulkDeleteUsersCommand : IRequest<BulkOperationResult>
{
    [Required]
    [MinLength(1)]
    public IList<Guid> UserIds { get; set; } = new List<Guid>();

    public bool SoftDelete { get; set; } = true;

    public string? Reason { get; set; }
}

/// <summary>
/// Command to bulk restore users
/// </summary>
public sealed class BulkRestoreUsersCommand : IRequest<BulkOperationResult>
{
    [Required]
    [MinLength(1)]
    public IList<Guid> UserIds { get; set; } = new List<Guid>();

    public string? Reason { get; set; }
}

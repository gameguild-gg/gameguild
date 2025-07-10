using System.ComponentModel.DataAnnotations;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to update user information with validation and business logic
/// </summary>
public sealed class UpdateUserCommand : IRequest<User> {
  [Required] public Guid UserId { get; set; }

  [StringLength(100, MinimumLength = 1)] public string? Name { get; set; }

  [EmailAddress] [StringLength(255)] public string? Email { get; set; }

  public bool? IsActive { get; set; }

  /// <summary>
  /// Expected version for optimistic concurrency control
  /// </summary>
  public int? ExpectedVersion { get; set; }
}

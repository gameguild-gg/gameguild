using System.ComponentModel.DataAnnotations;
using GameGuild.Common;

namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get user by ID
/// </summary>
public sealed class GetUserByIdQuery : IQuery<User?>
{
  [Required]
  public Guid UserId { get; set; }

  public bool IncludeDeleted { get; set; } = false;
}

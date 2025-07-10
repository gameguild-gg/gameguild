using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get user by email
/// </summary>
public sealed class GetUserByEmailQuery : IQuery<User?> {
  [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

  public bool IncludeDeleted { get; set; } = false;
}

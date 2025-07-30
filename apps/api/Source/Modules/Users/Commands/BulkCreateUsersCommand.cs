using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to create multiple users at once
/// </summary>
public sealed class BulkCreateUsersCommand : ICommand<BulkOperationResult> {
  [Required] public List<CreateUserDto> Users { get; init; } = [];

  public string? Reason { get; init; }
}

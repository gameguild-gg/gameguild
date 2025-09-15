using GameGuild;
using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to create multiple users at once
/// </summary>
public sealed class BulkCreateUsersCommand : IResultCommand<BulkOperationResult>
{
  [Required] public List<CreateUserDto> Users { get; init; } = [];

  public string? Reason { get; init; }
}

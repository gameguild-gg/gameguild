using GameGuild;
using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to deactivate multiple users at once
/// </summary>
public sealed class BulkDeactivateUsersCommand : IResultCommand<BulkOperationResult>
{
  [Required] public List<Guid> UserIds { get; set; } = [];

  public string? Reason { get; set; }
}

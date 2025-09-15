using GameGuild.CQRS;
using GameGuild;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to activate multiple users at once
/// </summary>
public sealed class BulkActivateUsersCommand : IResultCommand<BulkOperationResult> {
  [Required] public List<Guid> UserIds { get; init; } = [];

  public string? Reason { get; init; }
}

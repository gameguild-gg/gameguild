using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to bulk restore users
/// </summary>
public sealed class BulkRestoreUsersCommand : IResultCommand<BulkOperationResult> {
  [Required][MinLength(1)] public IList<Guid> UserIds { get; set; } = new List<Guid>();

  public string? Reason { get; set; }
}

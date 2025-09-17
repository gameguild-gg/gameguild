using GameGuild;
using GameGuild.CQRS;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to delete multiple users at once (soft or hard delete)
/// </summary>
public sealed class BulkDeleteUsersCommand : IResultCommand<BulkOperationResult> {
  [Required] [MinLength(1)] public IList<Guid> UserIds { get; set; } = new List<Guid>();

  public bool SoftDelete { get; set; } = true;

  public string? Reason { get; set; }
}

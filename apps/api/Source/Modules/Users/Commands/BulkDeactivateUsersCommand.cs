using GameGuild.Common;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to deactivate multiple users at once
/// </summary>
public sealed class BulkDeactivateUsersCommand : ICommand<BulkOperationResult> {
  [Required] public List<Guid> UserIds { get; set; } = [];

  public string? Reason { get; set; }
}

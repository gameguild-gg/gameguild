using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Common.Models;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to deactivate multiple users at once
/// </summary>
public sealed class BulkDeactivateUsersCommand : ICommand<BulkOperationResult> {
  [Required] public List<Guid> UserIds { get; set; } = new();

  public string? Reason { get; set; }
}

using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Common.Models;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to activate multiple users at once
/// </summary>
public sealed class BulkActivateUsersCommand : ICommand<BulkOperationResult> {
  [Required] public List<Guid> UserIds { get; init; } = [];

  public string? Reason { get; init; }
}

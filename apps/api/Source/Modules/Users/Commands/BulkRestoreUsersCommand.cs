using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to bulk restore users
/// </summary>
public sealed class BulkRestoreUsersCommand : IRequest<BulkOperationResult> {
  [Required] [MinLength(1)] public IList<Guid> UserIds { get; set; } = new List<Guid>();

  public string? Reason { get; set; }
}

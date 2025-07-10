using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Models;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to bulk delete users
/// </summary>
public sealed class BulkDeleteUsersCommand : IRequest<BulkOperationResult> {
  [Required] [MinLength(1)] public IList<Guid> UserIds { get; set; } = new List<Guid>();

  public bool SoftDelete { get; set; } = true;

  public string? Reason { get; set; }
}

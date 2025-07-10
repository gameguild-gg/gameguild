using System.ComponentModel.DataAnnotations;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to restore a soft-deleted user
/// </summary>
public class RestoreUserCommand : IRequest<bool> {
  [Required] public Guid UserId { get; set; }
}

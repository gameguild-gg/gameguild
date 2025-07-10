using System.ComponentModel.DataAnnotations;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to activate a user
/// </summary>
public sealed class ActivateUserCommand : IRequest<bool> {
  [Required] public Guid UserId { get; set; }

  public string? Reason { get; set; }
}

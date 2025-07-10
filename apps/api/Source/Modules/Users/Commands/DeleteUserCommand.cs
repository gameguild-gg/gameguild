using System.ComponentModel.DataAnnotations;
using MediatR;


namespace GameGuild.Modules.Users;

/// <summary>
/// Command to delete a user
/// </summary>
public class DeleteUserCommand : IRequest<bool>
{
  [Required]
  public Guid UserId { get; set; }

  public bool SoftDelete { get; set; } = true;
}
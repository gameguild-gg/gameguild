using GameGuild.Modules.Users.Models;
using MediatR;


namespace GameGuild.Modules.Authentication.Commands;

/// <summary>
/// Command to update the user profile with validation
/// </summary>
public class UpdateUserProfileCommand : IRequest<User> {
  public Guid UserId { get; set; }

  public string? Name { get; set; }

  public string? Email { get; set; }

  public bool? IsActive { get; set; }
}

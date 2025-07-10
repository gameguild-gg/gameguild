using GameGuild.Modules.Users;
using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Query to get user by email
/// </summary>
public class GetUserByEmailQuery : IRequest<User?> {
  public string Email { get; set; } = string.Empty;
}

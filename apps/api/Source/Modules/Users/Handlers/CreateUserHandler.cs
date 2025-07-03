using MediatR;
using GameGuild.Modules.User.Commands;
using GameGuild.Modules.User.Services;


namespace GameGuild.Modules.User.Handlers;

/// <summary>
/// Handler for creating a new user
/// </summary>
public class CreateUserHandler(IUserService userService) : IRequestHandler<CreateUserCommand, Models.User> {
  public async Task<Models.User> Handle(CreateUserCommand request, CancellationToken cancellationToken) {
    var user = new Models.User { Name = request.Name, Email = request.Email, IsActive = request.IsActive };

    return await userService.CreateUserAsync(user);
  }
}

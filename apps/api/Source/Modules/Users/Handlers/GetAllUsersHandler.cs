using GameGuild.Modules.Users.Queries;
using GameGuild.Modules.Users.Services;
using MediatR;


namespace GameGuild.Modules.Users.Handlers;

/// <summary>
/// Handler for getting all users
/// </summary>
public class GetAllUsersHandler(IUserService userService) : IRequestHandler<GetAllUsersQuery, IEnumerable<Models.User>> {
  public async Task<IEnumerable<Models.User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken) {
    if (request.IncludeDeleted) return await userService.GetDeletedUsersAsync();

    return await userService.GetAllUsersAsync();
  }
}

using MediatR;
using GameGuild.Modules.User.Queries;
using GameGuild.Modules.User.Services;


namespace GameGuild.Modules.User.Handlers;

/// <summary>
/// Handler for getting all users
/// </summary>
public class GetAllUsersHandler(IUserService userService) : IRequestHandler<GetAllUsersQuery, IEnumerable<Models.User>> {
  public async Task<IEnumerable<Models.User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken) {
    if (request.IncludeDeleted) return await userService.GetDeletedUsersAsync();

    return await userService.GetAllUsersAsync();
  }
}

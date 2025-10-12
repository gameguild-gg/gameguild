using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Handler for getting all users with filtering and pagination </summary>
public class GetAllUsersHandler(IUserService userService) : IRequestHandler<GetAllUsersQuery, IEnumerable<User>>
{
    private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));

    public async Task<IEnumerable<User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        // For now, use the basic GetAllUsersAsync - we can enhance this later for filtering
        return await _userService.GetAllUsersAsync();
    }
}

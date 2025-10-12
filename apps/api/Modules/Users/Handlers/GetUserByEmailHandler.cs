using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Handler for getting user by email </summary>
public class GetUserByEmailHandler(IUserService userService) : IQueryHandler<GetUserByEmailQuery, User?>
{
    private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));

    public async Task<User?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken) { return await _userService.GetByEmailAsync(request.Email); }
}

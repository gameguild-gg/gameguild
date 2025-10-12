using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to check if a user exists by email address
/// </summary>
public sealed class UserExistsByEmailQuery : IRequest<bool>
{
    /// <summary>
    ///     Email address to check
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
///     Handler for UserExistsByEmailQuery
/// </summary>
public sealed class UserExistsByEmailQueryHandler : IRequestHandler<UserExistsByEmailQuery, bool>
{
    private readonly IUserRepository _userRepository;

    public UserExistsByEmailQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(UserExistsByEmailQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
    }
}

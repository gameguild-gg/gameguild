using GameGuild.Modules.Users;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to get multiple users by their email addresses
/// </summary>
public sealed class GetUsersByEmailsQuery : IRequest<IEnumerable<UserDto>>
{
    /// <summary>
    ///     Collection of email addresses to retrieve
    /// </summary>
    public required IEnumerable<string> Emails { get; init; }
}

/// <summary>
///     Handler for GetUsersByEmailsQuery
/// </summary>
public sealed class GetUsersByEmailsQueryHandler : IRequestHandler<GetUsersByEmailsQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByEmailsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersByEmailsQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByEmailsAsync(request.Emails, cancellationToken);

        return users.Select(user => new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            GivenName = user.GivenName,
            FamilyName = user.FamilyName,
            DisplayName = user.DisplayName,
            Title = user.Title,
            Description = user.Description,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }
}

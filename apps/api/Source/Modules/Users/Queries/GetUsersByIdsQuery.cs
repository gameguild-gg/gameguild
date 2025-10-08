using GameGuild.Modules.Users.DTOs;
using MediatR;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to get multiple users by their unique identifiers
/// </summary>
public sealed class GetUsersByIdsQuery : IRequest<IEnumerable<UserDto>>
{
    /// <summary>
    ///     Collection of user IDs to retrieve
    /// </summary>
    public required IEnumerable<Guid> UserIds { get; init; }
}

/// <summary>
///     Handler for GetUsersByIdsQuery
/// </summary>
public sealed class GetUsersByIdsQueryHandler : IRequestHandler<GetUsersByIdsQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByIdsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersByIdsQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByIdsAsync(request.UserIds, cancellationToken);

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

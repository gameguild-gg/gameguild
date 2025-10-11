using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to search users with pagination
/// </summary>
public sealed class SearchUsersQuery : IRequest<PagedResult<UserDto>> {
    /// <summary>
    ///     Search term to match against name or email
    /// </summary>
    public required string SearchTerm { get; init; }

    /// <summary>
    ///     Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    ///     Number of users per page
    /// </summary>
    public int PageSize { get; init; } = 10;
}

/// <summary>
///     Handler for SearchUsersQuery
/// </summary>
public sealed class SearchUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<SearchUsersQuery, PagedResult<UserDto>> {
    public async Task<PagedResult<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken) {
        var (users, totalCount) = await userRepository.SearchAsync(
          request.SearchTerm,
          request.PageNumber,
          request.PageSize,
          cancellationToken
        );

        var userDtos = users.Select(user => new UserDto {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            GivenName = user.GivenName,
            FamilyName = user.FamilyName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }).ToList();

        return new PagedResult<UserDto>(
          userDtos,
          totalCount,
          request.PageNumber,
          request.PageSize
        );
    }
}

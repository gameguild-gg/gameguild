using GameGuild.Modules.Users.DTOs;
using GameGuild.Modules.Users.Models;
using MediatR;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to search users with pagination
/// </summary>
public sealed class SearchUsersQuery : IRequest<PagedResult<UserDto>>
{
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
public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public SearchUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var (users, totalCount) = await _userRepository.SearchAsync(
            request.SearchTerm,
            request.PageNumber,
            request.PageSize,
            cancellationToken
        );

        var userDtos = users.Select(user => new UserDto
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
        }).ToList();

        return new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}

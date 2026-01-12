using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query handler for getting users with pagination
/// </summary>
public class GetUsersPagedQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUsersPagedQuery, Models.PagedResult<UserDto>>
{
    public async Task<Models.PagedResult<UserDto>> Handle(GetUsersPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        (var users, var totalCount) = await userRepository.GetUsersPagedAsync(request.IsActive, request.PageNumber, request.PageSize, cancellationToken).ConfigureAwait(false);

        var userDtos = users.Select(user => new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt)).ToList();

        return new Models.PagedResult<UserDto>(userDtos, totalCount, request.PageNumber, request.PageSize);
    }
}

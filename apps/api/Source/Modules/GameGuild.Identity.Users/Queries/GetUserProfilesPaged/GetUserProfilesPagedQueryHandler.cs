using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query handler for getting user profiles with pagination, search, and sorting
/// </summary>
public sealed class GetUserProfilesPagedQueryHandler(IUserProfileRepository profileRepository)
    : IQueryHandler<GetUserProfilesPagedQuery, PagedResult<UserProfileDto>>
{
    public async Task<PagedResult<UserProfileDto>> Handle(GetUserProfilesPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get paginated profiles from repository
        var (profiles, totalCount) = await profileRepository.GetProfilesPagedAsync(
            request.Search,
            request.SortBy,
            request.SortDirection,
            request.PageNumber,
            request.PageSize,
            cancellationToken).ConfigureAwait(false);

        // Map to DTOs
        var profileDtos = profiles.Select(profile => new UserProfileDto(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.Bio,
            profile.Location,
            profile.Website,
            profile.AvatarUrl,
            profile.BannerUrl,
            TimeZone: null, // Not in entity yet
            Language: null, // Not in entity yet
            profile.Visibility.ToString().ToLowerInvariant(),
            ShowEmail: false, // Not in entity yet
            ShowLocation: false, // Not in entity yet
            profile.CreatedAt,
            profile.UpdatedAt,
            BitConverter.GetBytes(profile.Version) // Convert int version to byte array
        )).ToList();

        return new PagedResult<UserProfileDto>(profileDtos, totalCount, request.PageNumber, request.PageSize);
    }
}

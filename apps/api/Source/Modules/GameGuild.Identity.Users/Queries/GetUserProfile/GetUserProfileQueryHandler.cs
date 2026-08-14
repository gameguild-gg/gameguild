using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
/// Query handler for getting user profile by user ID
/// </summary>
public sealed class GetUserProfileQueryHandler(IUserProfileRepository profileRepository) : IQueryHandler<GetUserProfileQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
            return null;

        return UserProfileDto.FromEntity(profile);
    }
}

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

        return new UserProfileDto(
            Id: profile.Id,
            UserId: profile.UserId,
            DisplayName: profile.DisplayName,
            Bio: profile.Bio,
            Location: profile.Location,
            Website: profile.Website,
            JobTitle: profile.JobTitle,
            Company: profile.Company,
            AvatarUrl: profile.AvatarUrl,
            BannerUrl: profile.BannerUrl,
            TimeZone: null,
            Language: null,
            ProfileVisibility: profile.Visibility.ToString().ToLowerInvariant(),
            ShowEmail: false,
            ShowLocation: false,
            CreatedAt: profile.CreatedAt,
            UpdatedAt: profile.UpdatedAt,
            Version: BitConverter.GetBytes(profile.Version)
        );
    }
}

using GameGuild.CQRS;
using GameGuild.Identity.Users;

namespace GameGuild.Social.Profiles;

public class ReplaceUserProfileCommandHandler(IUserRepository userRepository, IUserProfileRepository profileRepository) : ICommandHandler<ReplaceUserProfileCommand>
{
    public async Task<Unit> Handle(ReplaceUserProfileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (profile == null)
        {
            profile = new UserProfile { UserId = request.UserId };
            await profileRepository.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        }

        // Replace all fields
        profile.DisplayName = request.Request.DisplayName;
        profile.Bio = request.Request.Bio;
        profile.Location = request.Request.Location;
        profile.Website = request.Request.Website;
        // Note: TimeZone, Language, ProfileVisibility, ShowEmail, ShowLocation are not properties of UserProfile
        // These appear to belong to UserPreferences instead

        profile.Touch();
        await profileRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

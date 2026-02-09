using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed class UpdateUserProfileCommandHandler(IUserRepository userRepository, IUserProfileRepository profileRepository) : ICommandHandler<UpdateUserProfileCommand>
{
    public async Task<Unit> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
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

        // Update only provided fields
        if (request.Request.DisplayName != null) profile.DisplayName = request.Request.DisplayName;
        if (request.Request.Bio != null) profile.Bio = request.Request.Bio;
        if (request.Request.Location != null) profile.Location = request.Request.Location;
        if (request.Request.Website != null) profile.Website = request.Request.Website;
        // Note: TimeZone, Language, ProfileVisibility, ShowEmail, ShowLocation are not properties of UserProfile
        // These appear to belong to UserPreferences instead

        profile.Touch();
        await profileRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

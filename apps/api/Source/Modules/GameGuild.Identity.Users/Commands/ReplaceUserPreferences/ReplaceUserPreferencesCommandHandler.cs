using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for replacing user preferences
/// </summary>
public class ReplaceUserPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<ReplaceUserPreferencesCommand>
{
    public async Task<Unit> Handle(ReplaceUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Verify user exists
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        // Get or create preferences
        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (preferences == null)
        {
            preferences = UserPreferences.Create(request.UserId);
            await preferencesRepository.AddAsync(preferences, cancellationToken).ConfigureAwait(false);
        }

        // Replace all preferences
        preferences.SetGeneralPreferences(request.Request.GeneralPreferences);
        preferences.SetNotificationPreferences(request.Request.NotificationPreferences);
        preferences.SetAccessibilityPreferences(request.Request.AccessibilityPreferences);
        preferences.SetPrivacyPreferences(request.Request.PrivacyPreferences);

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for updating user preferences
/// </summary>
public sealed class UpdateUserPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<UpdateUserPreferencesCommand>
{
    public async Task<Unit> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
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

        // Update fields (merge with existing)
        if (request.Request.GeneralPreferences != null)
        {
            var existing = preferences.GetGeneralPreferences();
            foreach (var pref in JsonValueDictionary.ToObjects(request.Request.GeneralPreferences))
            {
                existing[pref.Key] = pref.Value;
            }
            preferences.SetGeneralPreferences(existing);
        }

        if (request.Request.NotificationPreferences != null)
        {
            var existing = preferences.GetNotificationPreferences();
            foreach (var pref in JsonValueDictionary.ToObjects(request.Request.NotificationPreferences))
            {
                existing[pref.Key] = pref.Value;
            }
            preferences.SetNotificationPreferences(existing);
        }

        if (request.Request.AccessibilityPreferences != null)
        {
            var existing = preferences.GetAccessibilityPreferences();
            foreach (var pref in JsonValueDictionary.ToObjects(request.Request.AccessibilityPreferences))
            {
                existing[pref.Key] = pref.Value;
            }
            preferences.SetAccessibilityPreferences(existing);
        }

        if (request.Request.PrivacyPreferences != null)
        {
            var existing = preferences.GetPrivacyPreferences();
            foreach (var pref in JsonValueDictionary.ToObjects(request.Request.PrivacyPreferences))
            {
                existing[pref.Key] = pref.Value;
            }
            preferences.SetPrivacyPreferences(existing);
        }

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

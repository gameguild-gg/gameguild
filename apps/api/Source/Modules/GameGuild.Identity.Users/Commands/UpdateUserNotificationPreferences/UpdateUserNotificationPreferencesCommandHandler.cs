using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for updating user notification preferences
/// </summary>
public sealed class UpdateUserNotificationPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<UpdateUserNotificationPreferencesCommand>
{
    public async Task<Unit> Handle(UpdateUserNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (preferences == null)
        {
            preferences = UserPreferences.Create(request.UserId);
            await preferencesRepository.AddAsync(preferences, cancellationToken).ConfigureAwait(false);
        }

        var existing = preferences.GetNotificationPreferences();
        foreach (var pref in JsonValueDictionary.ToObjects(request.Request.NotificationPreferences))
        {
            existing[pref.Key] = pref.Value;
        }
        preferences.SetNotificationPreferences(existing);

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

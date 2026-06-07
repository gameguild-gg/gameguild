using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed class UpdateUserAccessibilityPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<UpdateUserAccessibilityPreferencesCommand>
{
    public async Task<Unit> Handle(UpdateUserAccessibilityPreferencesCommand request, CancellationToken cancellationToken)
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

        var existing = preferences.GetAccessibilityPreferences();
        foreach (var pref in JsonValueDictionary.ToObjects(request.Request.AccessibilityPreferences))
        {
            existing[pref.Key] = pref.Value;
        }
        preferences.SetAccessibilityPreferences(existing);

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

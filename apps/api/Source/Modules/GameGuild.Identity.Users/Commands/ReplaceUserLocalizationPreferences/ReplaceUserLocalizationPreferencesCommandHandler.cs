using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for replacing user localization preferences
/// </summary>
public sealed class ReplaceUserLocalizationPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository)
    : ICommandHandler<ReplaceUserLocalizationPreferencesCommand>
{
    public async Task<Unit> Handle(ReplaceUserLocalizationPreferencesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            throw new UserNotFoundException(request.UserId);
        }

        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (preferences == null)
        {
            preferences = UserPreferences.Create(request.UserId);
            await preferencesRepository.AddAsync(preferences, cancellationToken).ConfigureAwait(false);
        }

        // Preferred-currency validation (issue #412 spec E): unknown ISO 4217 codes fail
        // closed, known codes are stored normalized (upper-case).
        preferences.SetLocalizationPreferences(
            UserCurrencyPreference.NormalizeAndValidate(
                JsonValueDictionary.ToObjects(request.Request.LocalizationPreferences)));

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

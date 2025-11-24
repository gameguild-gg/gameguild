using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;

namespace GameGuild.Users.Commands;

public class UpdateUserPrivacyPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<UpdateUserPrivacyPreferencesCommand>
{
    public async Task<Unit> Handle(UpdateUserPrivacyPreferencesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (preferences == null)
        {
            preferences = Entities.UserPreferences.Create(request.UserId);
            await preferencesRepository.AddAsync(preferences, cancellationToken).ConfigureAwait(false);
        }

        var existing = preferences.GetPrivacyPreferences();
        foreach (var pref in request.Request.PrivacyPreferences)
        {
            existing[pref.Key] = pref.Value;
        }
        preferences.SetPrivacyPreferences(existing);

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

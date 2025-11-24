using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for resetting user localization preferences to defaults
/// </summary>
public class ResetUserLocalizationPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository)
    : ICommandHandler<ResetUserLocalizationPreferencesCommand>
{
    public async Task<Unit> Handle(ResetUserLocalizationPreferencesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var preferences = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (preferences == null)
        {
            // No preferences exist, nothing to reset
            return Unit.Value;
        }

        preferences.SetLocalizationPreferences(new Dictionary<string, object?>());

        await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for resetting user preferences to defaults
/// </summary>
public sealed class ResetUserPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<ResetUserPreferencesCommand>
{
    public async Task<Unit> Handle(ResetUserPreferencesCommand request, CancellationToken cancellationToken)
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
        else
        {
            preferences.ResetToDefaults();
            await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        }

        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

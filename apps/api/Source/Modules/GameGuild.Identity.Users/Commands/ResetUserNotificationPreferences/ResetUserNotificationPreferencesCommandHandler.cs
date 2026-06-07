using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed class ResetUserNotificationPreferencesCommandHandler(IUserRepository userRepository, IUserPreferencesRepository preferencesRepository) : ICommandHandler<ResetUserNotificationPreferencesCommand>
{
    public async Task<Unit> Handle(ResetUserNotificationPreferencesCommand request, CancellationToken cancellationToken)
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
        else
        {
            preferences.SetNotificationPreferences(new Dictionary<string, object?>());
            await preferencesRepository.UpdateAsync(preferences, cancellationToken).ConfigureAwait(false);
        }

        await preferencesRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

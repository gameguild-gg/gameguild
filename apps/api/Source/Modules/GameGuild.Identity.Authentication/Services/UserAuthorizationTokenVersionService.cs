using GameGuild.Identity.Authorization;
using GameGuild.Identity.Users;

namespace GameGuild.Identity.Authentication;

/// <summary>Advances the persisted token version so previously issued authorization claims stop being accepted.</summary>
public sealed class UserAuthorizationTokenVersionService(IUserRepository userRepository) : IUserAuthorizationTokenVersionService
{
    public Task IncrementAsync(Guid userId)
        => IncrementAsync(userId, CancellationToken.None);

    public async Task IncrementAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.", nameof(userId));
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return;
        }

        user.IncrementTokenVersion();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task IncrementManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var uniqueUserIds = userIds.Distinct().ToArray();
        if (uniqueUserIds.Any(userId => userId == Guid.Empty))
        {
            throw new ArgumentException("User IDs cannot be empty.", nameof(userIds));
        }

        if (uniqueUserIds.Length == 0)
        {
            return;
        }

        var users = (await userRepository.GetByIdsAsync(uniqueUserIds, cancellationToken).ConfigureAwait(false)).ToArray();
        foreach (var user in users)
        {
            user.IncrementTokenVersion();
            await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }

        if (users.Length > 0)
        {
            await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

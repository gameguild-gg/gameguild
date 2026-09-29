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
}

using GameGuild.Identity.Users;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Resolver seam decision: the module already references GameGuild.Identity.Users and the API host registers
/// <see cref="IUserRepository"/> (same seam MonthlyStatementDispatchBackgroundService resolves it through),
/// so the resolver queries the user store directly instead of a wired delegate.
/// </summary>
public sealed class RecipientEmailResolver(IUserRepository userRepository) : IRecipientEmailResolver
{
    public async Task<string?> ResolveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(notification.RecipientEmail))
        {
            return notification.RecipientEmail;
        }

        if (notification.RecipientId is { } userId)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                return user.Email;
            }
        }

        return null;
    }
}

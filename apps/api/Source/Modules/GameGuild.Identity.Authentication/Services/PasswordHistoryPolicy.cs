using GameGuild.Identity.Users;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Checks a candidate password against the current password and recent password history.
/// </summary>
internal static class PasswordHistoryPolicy
{
    public static bool WasRecentlyUsed(User user, string candidatePassword, IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        if (!string.IsNullOrWhiteSpace(user.PasswordHash) && passwordHasher.VerifyPassword(user.PasswordHash, candidatePassword))
        {
            return true;
        }

        return user.GetPasswordHistoryHashes().Any(hash => passwordHasher.VerifyPassword(hash, candidatePassword));
    }
}

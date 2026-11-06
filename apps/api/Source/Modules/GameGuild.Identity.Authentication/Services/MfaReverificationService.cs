using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Default <see cref="IMfaReverificationService"/>: mirrors the password proof the
///     MFA disable flow requires (MfaService.DisableMfaAsync, issue #244) so every
///     MFA-state mutation applies the same step-up rule.
/// </summary>
public sealed class MfaReverificationService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<MfaReverificationService> logger) : IMfaReverificationService
{
    public async Task<bool> VerifyPasswordAsync(Guid userId, string password, string operation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user?.PasswordHash is not { } passwordHash)
        {
            logger.LogWarning("MFA step-up refused for user {UserId} ({Operation}): no password credential on file", userId, operation);
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, "reverify-refused");
            return false;
        }

        if (!passwordHasher.VerifyPassword(passwordHash, password))
        {
            logger.LogWarning("MFA step-up refused for user {UserId} ({Operation}): password verification failed", userId, operation);
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, "reverify-refused");
            return false;
        }

        return true;
    }
}

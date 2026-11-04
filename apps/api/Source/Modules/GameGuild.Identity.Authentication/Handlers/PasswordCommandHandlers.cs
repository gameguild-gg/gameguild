using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

public sealed class VerifyEmailCommandHandler(
    IEmailVerificationService emailVerificationService,
    IUserRepository userRepository,
    ILogger<VerifyEmailCommandHandler> logger) : ICommandHandler<VerifyEmailCommand, EmailVerificationResult>
{
    public async Task<EmailVerificationResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var validation = await emailVerificationService.VerifyEmailTokenAsync(request.Token).ConfigureAwait(false);
        if (!validation.Success || validation.UserId is not { } userId)
        {
            return new EmailVerificationResult
            {
                Success = false,
                Message = validation.FailureReason ?? "Invalid or expired verification token"
            };
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return new EmailVerificationResult
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (!user.IsEmailVerified)
        {
            user.VerifyEmail();
            await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Email verified for user {UserId}", userId);

        return new EmailVerificationResult
        {
            Success = true,
            Message = "Email verified successfully",
            Email = validation.Email ?? user.Email,
            UserId = userId,
            VerifiedAt = SystemClock.UtcNow
        };
    }
}

public sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationService emailVerificationService,
    IPublisher publisher,
    ILogger<RequestPasswordResetCommandHandler> logger) : ICommandHandler<RequestPasswordResetCommand, PasswordResetRequestResult>
{
    public async Task<PasswordResetRequestResult> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (user is not null && !user.IsEmailVerified)
        {
            // Account-recovery takeover guard (issue #1124): an attacker can register with a
            // victim's address and never verify it; a reset link would then be delivered to
            // the address they do not own ONLY if the mailbox were theirs — but for never-
            // verified addresses ownership was never proven, so recovery is refused outright.
            // The response stays identical to the known-account path so the refusal itself
            // does not become a verification-status oracle.
            logger.LogInformation(
                "Password reset refused for never-verified email (user {UserId}) from {IpAddress}",
                user.Id,
                request.IpAddress ?? "unknown");
        }
        else if (user is not null)
        {
            var token = await emailVerificationService.GeneratePasswordResetTokenAsync(user.Id, user.Email).ConfigureAwait(false);
            await publisher.Publish(
                new PasswordResetRequestedNotification
                {
                    Email = user.Email,
                    Token = token,
                    UserName = user.Username ?? user.Name
                },
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Password reset token generated for user {UserId} from {IpAddress}",
                user.Id,
                request.IpAddress ?? "unknown");
        }
        else
        {
            logger.LogInformation("Password reset requested for unknown email from {IpAddress}", request.IpAddress ?? "unknown");
        }

        return new PasswordResetRequestResult
        {
            Success = true,
            Message = "If an account with that email exists, a password reset link has been sent.",
            ExpiresInMinutes = 60
        };
    }
}

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailVerificationService emailVerificationService,
    ILogger<ResetPasswordCommandHandler> logger,
    IUserSessionRepository? userSessionRepository = null,
    SessionOptions? sessionOptions = null,
    IPasswordChangeRevocationService? passwordRevocation = null,
    IOtherSessionsTerminatedAlertPublisher? sessionTerminatedAlerts = null) : ICommandHandler<ResetPasswordCommand, PasswordResetResult>
{
    public async Task<PasswordResetResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return new PasswordResetResult { Success = false, Message = "Password confirmation does not match" };
        }

        var strength = passwordHasher.ValidatePasswordStrength(request.NewPassword);
        if (!strength.IsValid)
        {
            return new PasswordResetResult
            {
                Success = false,
                Message = string.Join("; ", strength.ValidationFailures)
            };
        }

        var validation = await emailVerificationService.VerifyPasswordResetTokenAsync(request.Token).ConfigureAwait(false);
        if (!validation.Success || validation.UserId is not { } userId)
        {
            return new PasswordResetResult
            {
                Success = false,
                Message = validation.FailureReason ?? "Invalid or expired reset token"
            };
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return new PasswordResetResult
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (!user.IsEmailVerified)
        {
            // Issue #1124 (defense in depth): reset tokens are no longer issued for
            // never-verified addresses, but any token issued by an older or parallel path
            // is refused here as well — an unverified address must never unlock recovery.
            logger.LogInformation("Password reset refused at consume time for never-verified user {UserId}", userId);
            return new PasswordResetResult
            {
                Success = false,
                Message = "Invalid or expired reset token"
            };
        }

        if (PasswordHistoryPolicy.WasRecentlyUsed(user, request.NewPassword, passwordHasher))
        {
            return new PasswordResetResult { Success = false, Message = "You cannot reuse any of your last 5 passwords." };
        }

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        var updated = await userRepository.UpdatePasswordHashAsync(userId, passwordHash, user.PasswordHash, cancellationToken).ConfigureAwait(false);
        if (!updated)
        {
            return new PasswordResetResult { Success = false, Message = "Your password changed during this reset. Request a new password reset link and try again." };
        }

        if (sessionOptions?.TerminateSessionsOnPasswordChange == true)
        {
            var resetRevokedSessions = 0;
            if (passwordRevocation is not null)
            {
                // Full revocation battery (issue #278): refresh tokens, sessions and the
                // versioned cutoff covering legacy access tokens, all audited.
                resetRevokedSessions = await passwordRevocation.RevokeAllSessionsAndTokensAsync(userId, "Password reset", cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (userSessionRepository is not null)
            {
                var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
                resetRevokedSessions = activeSessions.Count;
                await userSessionRepository.TerminateAllForUserAsync(
                    userId,
                    SessionTerminationReason.SecurityViolation.ToString(),
                    cancellationToken).ConfigureAwait(false);
            }

            // Issue #1091: the owner is notified that every other session was terminated
            // by this password reset, instead of silently losing signed-in devices.
            if (sessionTerminatedAlerts is not null)
            {
                await sessionTerminatedAlerts.RecordAsync(
                    userId,
                    tenantId: null,
                    SessionTerminationReason.PasswordChanged.ToString(),
                    resetRevokedSessions,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Password reset completed for user {UserId}", userId);

        return new PasswordResetResult
        {
            Success = true,
            Message = "Password reset successfully",
            UserId = userId
        };
    }
}

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<ChangePasswordCommandHandler> logger,
    IUserSessionRepository? userSessionRepository = null,
    SessionOptions? sessionOptions = null,
    IPasswordChangeRevocationService? passwordRevocation = null,
    IOtherSessionsTerminatedAlertPublisher? sessionTerminatedAlerts = null) : ICommandHandler<ChangePasswordCommand, PasswordChangeResult>
{
    public async Task<PasswordChangeResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return new PasswordChangeResult { Success = false, Message = "Password confirmation does not match" };
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return new PasswordChangeResult { Success = false, Message = "User not found" };
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            // Set-initial mode (OAuth-only account): there is no current password to verify,
            // but a supplied one can never match, so it is still rejected.
            if (!string.IsNullOrEmpty(request.CurrentPassword))
            {
                return new PasswordChangeResult { Success = false, Message = "Current password is incorrect" };
            }
        }
        else if (!passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
        {
            return new PasswordChangeResult { Success = false, Message = "Current password is incorrect" };
        }

        var strength = passwordHasher.ValidatePasswordStrength(request.NewPassword);
        if (!strength.IsValid)
        {
            return new PasswordChangeResult
            {
                Success = false,
                Message = string.Join("; ", strength.ValidationFailures)
            };
        }

        if (PasswordHistoryPolicy.WasRecentlyUsed(user, request.NewPassword, passwordHasher))
        {
            return new PasswordChangeResult { Success = false, Message = "You cannot reuse any of your last 5 passwords." };
        }

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        var updated = await userRepository.UpdatePasswordHashAsync(request.UserId, passwordHash, user.PasswordHash, cancellationToken).ConfigureAwait(false);
        if (!updated)
        {
            return new PasswordChangeResult { Success = false, Message = "Your password changed during this request. Sign in again and retry." };
        }

        var revokedSessions = 0;
        if (sessionOptions?.TerminateSessionsOnPasswordChange == true)
        {
            if (passwordRevocation is not null)
            {
                // Full revocation battery (issue #278): refresh tokens, sessions and the
                // versioned cutoff covering legacy access tokens, all audited.
                revokedSessions = await passwordRevocation.RevokeAllSessionsAndTokensAsync(
                        request.UserId, "Password changed", cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (userSessionRepository is not null)
            {
                var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
                revokedSessions = activeSessions.Count;
                await userSessionRepository.TerminateAllForUserAsync(
                    request.UserId,
                    SessionTerminationReason.SecurityViolation.ToString(),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        else if (request.RevokeOtherSessions && userSessionRepository is not null && request.CurrentSessionId is { } keepSessionId)
        {
            var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            revokedSessions = activeSessions.Count(s => s.Id != keepSessionId);
            await userSessionRepository.TerminateAllExceptAsync(
                request.UserId,
                keepSessionId,
                "password_changed",
                cancellationToken).ConfigureAwait(false);
        }
        else if (request.RevokeOtherSessions)
        {
            logger.LogInformation("Skipping session revocation for user {UserId}: no session id or session repository", request.UserId);
        }

        // Issue #1091: the owner is notified that all other sessions were terminated by
        // this password change, instead of silently losing every other signed-in device.
        if (sessionTerminatedAlerts is not null && revokedSessions > 0)
        {
            await sessionTerminatedAlerts.RecordAsync(
                request.UserId,
                tenantId: null,
                SessionTerminationReason.PasswordChanged.ToString(),
                revokedSessions,
                cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Password changed for user {UserId}; revoked sessions: {RevokedSessions}", request.UserId, revokedSessions);

        return new PasswordChangeResult
        {
            Success = true,
            Message = "Password changed successfully",
            SessionsRevoked = revokedSessions
        };
    }
}

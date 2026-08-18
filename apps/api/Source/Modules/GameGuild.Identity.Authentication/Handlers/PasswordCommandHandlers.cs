using GameGuild.CQRS;
using GameGuild.Identity.Users;
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
        if (user is not null)
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
    ILogger<ResetPasswordCommandHandler> logger) : ICommandHandler<ResetPasswordCommand, PasswordResetResult>
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

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        await userRepository.UpdatePasswordHashAsync(userId, passwordHash, cancellationToken).ConfigureAwait(false);

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
    IUserSessionRepository? userSessionRepository = null) : ICommandHandler<ChangePasswordCommand, PasswordChangeResult>
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

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        await userRepository.UpdatePasswordHashAsync(request.UserId, passwordHash, cancellationToken).ConfigureAwait(false);

        var revokedSessions = 0;
        if (request.RevokeOtherSessions)
        {
            if (request.CurrentSessionId is { } keepSessionId && userSessionRepository is not null)
            {
                var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
                revokedSessions = activeSessions.Count(s => s.Id != keepSessionId);
                await userSessionRepository.TerminateAllExceptAsync(request.UserId, keepSessionId, "password_changed", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation("Skipping session revocation for user {UserId}: no session id in token", request.UserId);
            }
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

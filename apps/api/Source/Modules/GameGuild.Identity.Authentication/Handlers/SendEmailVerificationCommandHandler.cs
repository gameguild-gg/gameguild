using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles the SendEmailVerificationCommand by generating a verification token
///     and sending the verification email via IEmailVerificationService.
/// </summary>
public sealed class SendEmailVerificationCommandHandler(
    IEmailVerificationService emailVerificationService,
    IUserRepository userRepository,
    ILogger<SendEmailVerificationCommandHandler> logger
) : ICommandHandler<SendEmailVerificationCommand, EmailVerificationResponse>
{
    public async Task<EmailVerificationResponse> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = request.UserId.HasValue
            ? await userRepository.GetByIdAsync(request.UserId.Value, cancellationToken).ConfigureAwait(false)
            : await userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            logger.LogInformation("Email verification requested for unknown email {Email}", email);
            return new EmailVerificationResponse { Message = "If an account exists with that email, a verification email has been sent" };
        }

        logger.LogInformation("Sending email verification for user {UserId}", user.Id);

        var token = await emailVerificationService.GenerateVerificationTokenAsync(user.Id, user.Email).ConfigureAwait(false);
        await emailVerificationService.SendVerificationEmailAsync(user.Email, token, request.UserName ?? user.Username ?? user.Name).ConfigureAwait(false);

        return new EmailVerificationResponse { Message = "Verification email sent successfully" };
    }
}

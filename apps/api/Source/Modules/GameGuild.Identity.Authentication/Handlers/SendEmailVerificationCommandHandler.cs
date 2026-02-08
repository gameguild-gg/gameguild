using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles the SendEmailVerificationCommand by generating a verification token
///     and sending the verification email via IEmailVerificationService.
/// </summary>
public sealed class SendEmailVerificationCommandHandler(
    IEmailVerificationService emailVerificationService,
    ILogger<SendEmailVerificationCommandHandler> logger
) : ICommandHandler<SendEmailVerificationCommand, EmailVerificationResponse>
{
    public async Task<EmailVerificationResponse> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? Guid.NewGuid();

        logger.LogInformation("Sending email verification for {Email}", request.Email);

        var token = await emailVerificationService.GenerateVerificationTokenAsync(userId, request.Email).ConfigureAwait(false);
        await emailVerificationService.SendVerificationEmailAsync(request.Email, token).ConfigureAwait(false);

        return new EmailVerificationResponse { Message = "Verification email sent successfully" };
    }
}

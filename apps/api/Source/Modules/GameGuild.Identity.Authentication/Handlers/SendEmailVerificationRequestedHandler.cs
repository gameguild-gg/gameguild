using GameGuild.CQRS;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles verification email delivery for verification notifications.
/// </summary>
public sealed class SendEmailVerificationRequestedHandler(
    ILogger<SendEmailVerificationRequestedHandler> logger,
    IEmailSender? emailSender = null,
    IConfiguration? configuration = null) : INotificationHandler<EmailVerificationRequestedNotification>
{
    public async Task Handle(EmailVerificationRequestedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
            var verificationLink = $"{appBaseUrl.TrimEnd('/')}/verify-email?token={notification.Token}";

            if (emailSender == null)
            {
                logger.LogInformation(
                    "Email verification link for {Email} (User: {UserName}): {VerificationLink}",
                    notification.Email,
                    notification.UserName ?? "Unknown",
                    verificationLink);
                return;
            }

            var recipientName = string.IsNullOrWhiteSpace(notification.UserName) ? notification.Email : notification.UserName;
            var plainTextContent =
                $"Hi {recipientName},\n\nPlease verify your GameGuild email address by visiting:\n{verificationLink}\n\nIf you did not request this, you can ignore this email.";
            var htmlContent =
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p><p>Please verify your GameGuild email address by visiting the link below:</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(verificationLink)}\">Verify your email address</a></p><p>If you did not request this, you can ignore this email.</p>";

            await emailSender.SendAsync(
                new EmailMessage(
                    notification.Email,
                    "Verify your GameGuild email address",
                    plainTextContent,
                    htmlContent,
                    recipientName),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Verification email delivered to {Email}", notification.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending verification email to {Email}", notification.Email);
            throw;
        }
    }
}

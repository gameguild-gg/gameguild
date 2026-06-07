using GameGuild.CQRS;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles password reset email delivery.
/// </summary>
public sealed class SendPasswordResetRequestedHandler(
    ILogger<SendPasswordResetRequestedHandler> logger,
    IEmailSender? emailSender = null,
    IConfiguration? configuration = null) : INotificationHandler<PasswordResetRequestedNotification>
{
    public async Task Handle(PasswordResetRequestedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
            var resetLink = $"{appBaseUrl.TrimEnd('/')}/reset-password?token={notification.Token}";

            if (emailSender == null)
            {
                logger.LogInformation(
                    "Password reset link for {Email} (User: {UserName}): {ResetLink}",
                    notification.Email,
                    notification.UserName ?? "Unknown",
                    resetLink);
                return;
            }

            var recipientName = string.IsNullOrWhiteSpace(notification.UserName) ? notification.Email : notification.UserName;
            var encodedName = System.Net.WebUtility.HtmlEncode(recipientName);
            var encodedLink = System.Net.WebUtility.HtmlEncode(resetLink);

            var plainTextContent =
                $"Hi {recipientName},\n\nReset your GameGuild password by visiting:\n{resetLink}\n\nIf you did not request this, you can ignore this email.";
            var htmlContent =
                $"<p>Hi {encodedName},</p><p>Reset your GameGuild password by visiting the link below:</p><p><a href=\"{encodedLink}\">Reset your password</a></p><p>If you did not request this, you can ignore this email.</p>";

            await emailSender.SendAsync(
                new EmailMessage(
                    notification.Email,
                    "Reset your GameGuild password",
                    plainTextContent,
                    htmlContent,
                    recipientName),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Password reset email delivered to {Email}", notification.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending password reset email to {Email}", notification.Email);
            throw;
        }
    }
}

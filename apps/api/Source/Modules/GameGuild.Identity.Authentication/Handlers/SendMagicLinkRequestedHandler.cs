using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles magic sign-in link delivery by recording a notification row.
/// </summary>
public sealed class SendMagicLinkRequestedHandler(
    ILogger<SendMagicLinkRequestedHandler> logger,
    INotificationService notificationService,
    IUserRepository userRepository) : INotificationHandler<MagicLinkRequestedNotification>
{
    public async Task Handle(MagicLinkRequestedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userRepository.GetByEmailAsync(notification.Email, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                logger.LogWarning("Magic-link email requested for unknown email {Email}", notification.Email);
                return;
            }

            var metadata = JsonSerializer.Serialize(new
            {
                token = notification.Token,
                email = notification.Email,
                userName = notification.UserName
            });

            await notificationService.SendAsync(
                user.Id,
                NotificationType.MagicLink,
                "Your GameGuild sign-in link",
                "Sign in with this magic link.",
                NotificationChannel.Email,
                notification.TenantId,
                metadata: metadata,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Magic-link email queued for {Email}", notification.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error queueing magic-link email to {Email}", notification.Email);
        }
    }
}

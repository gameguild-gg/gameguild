using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Notifications.Services.Email.Handlers;

/// <summary>
/// Creates an email-channel Notification row for a tenant invite (new or resend). RecipientId is null
/// (the invitee may not have an account); the address lives on RecipientEmail. Row-creation failures are
/// logged, never thrown, so the invite command still succeeds when email infra is unavailable.
/// </summary>
public sealed class TenantInviteRequestedHandler(
    INotificationService notificationService,
    ILogger<TenantInviteRequestedHandler> logger) : IDomainEventHandler<TenantInviteRequestedNotification>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task Handle(TenantInviteRequestedNotification notification, CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            inviteeName = notification.InviteeName,
            invitedByEmail = notification.InvitedByEmail,
            tenantName = notification.TenantName,
            role = notification.Role,
            reviewUrl = notification.ReviewUrl,
            activationUrl = notification.ActivationUrl,
            resend = notification.Resend
        }, JsonOptions);

        try
        {
            var result = await notificationService.SendAsync(
                recipientId: null,
                type: NotificationType.TenantInvite,
                title: notification.Resend
                    ? $"Reminder: you were invited to {notification.TenantName} on GameGuild"
                    : $"You were invited to {notification.TenantName} on GameGuild",
                message: string.Empty,
                channel: NotificationChannel.Email,
                tenantId: notification.TenantId,
                metadata: metadata,
                recipientEmail: notification.InviteeEmail.Trim(),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to queue tenant invite notification. Email: {Email}, Reason: {Reason}",
                    notification.InviteeEmail, result.Error?.Description);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to queue tenant invite notification for {Email}", notification.InviteeEmail);
        }
    }
}

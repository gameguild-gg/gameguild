using System.Text.Json;
using GameGuild.Analytics;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for user signed up notifications - logs analytics event.
/// </summary>
public sealed class LogAnalyticsEventHandler(
    ILogger<LogAnalyticsEventHandler> logger,
    IAnalyticsService? analyticsService = null) : INotificationHandler<UserSignedUpNotification>
{
    public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken)
    {
        if (analyticsService != null)
        {
            var propertiesJson = JsonSerializer.Serialize(new
            {
                user_id = notification.UserId,
                email = notification.Email,
                username = notification.Username,
                tenant_id = notification.TenantId
            });

            await analyticsService.TrackEventAsync(
                "user_signed_up",
                propertiesJson,
                notification.UserId,
                notification.TenantId,
                cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Analytics: User sign-up event - UserId: {UserId}, Email: {Email}, Username: {Username}, TenantId: {TenantId}",
            notification.UserId,
            notification.Email,
            notification.Username,
            notification.TenantId
        );
    }
}

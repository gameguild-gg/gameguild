using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for user signed up notifications - logs analytics event
/// </summary>
public class LogAnalyticsEventHandler(ILogger<LogAnalyticsEventHandler> logger) : INotificationHandler<UserSignedUpNotification>
{
    // TODO: Inject IAnalyticsService when implemented

    public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken)
    {
        // In a real application, you would send this to an analytics service
        // like Google Analytics, Mixpanel, Segment, etc.
        logger.LogInformation(
            "Analytics: User sign-up event - UserId: {UserId}, Email: {Email}, Username: {Username}, TenantId: {TenantId}",
            notification.UserId,
            notification.Email,
            notification.Username,
            notification.TenantId
        );

        // TODO: Replace with actual analytics service call
        // await _analyticsService.TrackEventAsync("user_signed_up", new {
        //     user_id = notification.UserId,
        //     email = notification.Email,
        //     username = notification.Username,
        //     tenant_id = notification.TenantId
        // });

        // Simulate analytics API call
        await Task.Delay(50, cancellationToken);

        logger.LogInformation("Analytics event logged for user {Email}", notification.Email);
    }
}

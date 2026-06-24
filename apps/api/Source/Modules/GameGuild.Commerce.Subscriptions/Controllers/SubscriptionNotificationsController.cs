using Asp.Versioning;
using System.Diagnostics.CodeAnalysis;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Subscriptions;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications/subscriptions")]
[Microsoft.AspNetCore.Http.Tags("notifications/subscriptions")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
public sealed class SubscriptionNotificationsController(ISender sender) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("List subscription billing notifications")]
    [EndpointDescription("Lists local billing notification records tied to subscriptions.")]
    [ProducesResponseType<PagedResult<SubscriptionNotificationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionNotifications(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? subscriptionId,
        [FromQuery] NotificationChannel? channel,
        [FromQuery] bool? isSent,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        NormalizePaging(ref page, ref pageSize);

        var result = await sender.Send(
            new GetSubscriptionNotificationsQuery(tenantId, subscriptionId, channel, isSent, page, pageSize),
            ct).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("{notificationId:guid}:resend")]
    [EndpointSummary("Resend subscription billing notification")]
    [EndpointDescription("Creates a new local delivery record from an existing subscription billing notification.")]
    [ProducesResponseType<SubscriptionNotificationDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendSubscriptionNotification(
        Guid notificationId,
        [FromBody] ResendSubscriptionNotificationRequest body,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var result = await sender.Send(new ResendSubscriptionNotificationCommand(notificationId, body.Channel), ct).ConfigureAwait(false);
        return AcceptedAtAction(nameof(GetSubscriptionNotifications), new { subscriptionId = result.SubscriptionId }, result);
    }

    [ExcludeFromCodeCoverage]
    private static void NormalizePaging(ref int page, ref int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
    }

    public sealed record ResendSubscriptionNotificationRequest(NotificationChannel? Channel = null);
}

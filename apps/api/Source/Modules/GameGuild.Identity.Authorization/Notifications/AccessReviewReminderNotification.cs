using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Notification published when an access review reminder is sent to a reviewer.
///     Handlers can integrate with the Communication module's notification infrastructure
///     to deliver in-app or email notifications.
/// </summary>
public record AccessReviewReminderNotification(
    Guid CampaignId,
    Guid ItemId,
    Guid ReviewerId
) : INotification;

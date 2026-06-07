using System.Text.Json;
using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
/// Query handler for getting a single user notification
/// </summary>
public sealed class GetUserNotificationQueryHandler(IUserRepository userRepository, IUserNotificationRepository notificationRepository) : IQueryHandler<GetUserNotificationQuery, UserNotificationDetailDto?>
{
    public async Task<UserNotificationDetailDto?> Handle(GetUserNotificationQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (user is null) { return null; }

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken).ConfigureAwait(false);

        if (notification is null || notification.UserId != request.UserId || notification.DeletedAt is not null) { return null; }

        var relatedNotifications = await GetRelatedNotificationsAsync(notification, cancellationToken).ConfigureAwait(false);
        var actions = CreateActions(notification);

        return new UserNotificationDetailDto(MapNotification(notification), relatedNotifications, actions);
    }

    private async Task<List<UserNotificationDto>> GetRelatedNotificationsAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        if (!notification.RelatedEntityId.HasValue) { return []; }

        var candidates = await notificationRepository.GetByUserIdAsync(notification.UserId, skip: 0, take: 50, cancellationToken).ConfigureAwait(false);

        return candidates
            .Where(candidate => candidate.Id != notification.Id && candidate.RelatedEntityId == notification.RelatedEntityId)
            .Select(MapNotification)
            .ToList();
    }

    private static List<NotificationActionDto> CreateActions(UserNotification notification)
    {
        if (string.IsNullOrWhiteSpace(notification.ActionUrl)) { return []; }

        return
        [
            new NotificationActionDto(
                Id: "open",
                Text: "Open",
                Url: notification.ActionUrl,
                Type: "navigate",
                IsPrimary: true)
        ];
    }

    private static UserNotificationDto MapNotification(UserNotification notification)
        => new(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Content,
            notification.Priority.ToString().ToLowerInvariant(),
            notification.RelatedEntityType,
            notification.IsRead,
            notification.IsArchived,
            notification.ReadAt.HasValue ? new DateTimeOffset(notification.ReadAt.Value, TimeSpan.Zero) : null,
            notification.ArchivedAt.HasValue ? new DateTimeOffset(notification.ArchivedAt.Value, TimeSpan.Zero) : null,
            null,
            notification.ActionUrl,
            null,
            null,
            DeserializeMetadata(notification.Metadata),
            notification.CreatedAt,
            new DateTimeOffset(notification.UpdatedAt, TimeSpan.Zero),
            BitConverter.GetBytes(notification.Version));

    private static Dictionary<string, JsonElement> DeserializeMetadata(string? metadata)
        => string.IsNullOrWhiteSpace(metadata)
            ? new Dictionary<string, JsonElement>()
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadata) ?? new Dictionary<string, JsonElement>();
}

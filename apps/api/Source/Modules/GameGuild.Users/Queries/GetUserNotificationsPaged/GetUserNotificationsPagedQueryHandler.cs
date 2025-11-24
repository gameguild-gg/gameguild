using GameGuild.CQRS;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;
using System.Text.Json;

namespace GameGuild.Users.Queries;

/// <summary>
///     Query handler for getting user notifications with pagination, search, and filtering
/// </summary>
public class GetUserNotificationsPagedQueryHandler(IUserNotificationRepository notificationRepository)
    : IQueryHandler<GetUserNotificationsPagedQuery, PagedResult<UserNotificationDto>>
{
    public async Task<PagedResult<UserNotificationDto>> Handle(GetUserNotificationsPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get paginated notifications from repository
        var (notifications, totalCount) = await notificationRepository.GetPagedByUserIdAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDirection,
            request.IsArchived,
            request.Type,
            request.IsRead,
            request.Priority,
            request.FromDate?.DateTime,
            request.ToDate?.DateTime,
            cancellationToken).ConfigureAwait(false);

        // Map to DTOs
        var notificationDtos = notifications.Select(notification => new UserNotificationDto(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Content, // Message in DTO is Content in Entity
            notification.Priority.ToString().ToLowerInvariant(),
            null, // Category not in entity yet
            notification.IsRead,
            notification.IsArchived,
            notification.ReadAt.HasValue ? new DateTimeOffset(notification.ReadAt.Value, TimeSpan.Zero) : null,
            notification.ArchivedAt.HasValue ? new DateTimeOffset(notification.ArchivedAt.Value, TimeSpan.Zero) : null,
            null, // ExpiresAt not in entity yet
            notification.ActionUrl,
            null, // ActionText not in entity yet
            null, // ImageUrl not in entity yet
            string.IsNullOrEmpty(notification.Metadata)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(notification.Metadata) ?? new Dictionary<string, object?>(),
            notification.CreatedAt,
            new DateTimeOffset(notification.UpdatedAt, TimeSpan.Zero),
            BitConverter.GetBytes(notification.Version)
        )).ToList();

        return new PagedResult<UserNotificationDto>(notificationDtos, totalCount, request.PageNumber, request.PageSize);
    }
}

using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to mark multiple notifications as unread
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="NotificationIds">List of notification IDs to mark as unread</param>
public record BulkMarkNotificationsAsUnreadCommand(Guid UserId, List<Guid> NotificationIds) : ICommand;

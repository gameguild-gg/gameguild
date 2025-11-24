using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to mark multiple notifications as read
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="NotificationIds">List of notification IDs to mark as read</param>
public record BulkMarkNotificationsAsReadCommand(Guid UserId, List<Guid> NotificationIds) : ICommand;

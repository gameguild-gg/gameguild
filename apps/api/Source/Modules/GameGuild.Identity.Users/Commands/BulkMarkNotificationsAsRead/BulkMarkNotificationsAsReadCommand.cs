using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to mark multiple notifications as read
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="NotificationIds">List of notification IDs to mark as read</param>
public sealed record BulkMarkNotificationsAsReadCommand(Guid UserId, List<Guid> NotificationIds) : ICommand;

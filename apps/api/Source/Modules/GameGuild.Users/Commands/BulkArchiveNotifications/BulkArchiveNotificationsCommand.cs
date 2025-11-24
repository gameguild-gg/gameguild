using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to archive multiple notifications
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="NotificationIds">List of notification IDs to archive</param>
public record BulkArchiveNotificationsCommand(Guid UserId, List<Guid> NotificationIds) : ICommand;

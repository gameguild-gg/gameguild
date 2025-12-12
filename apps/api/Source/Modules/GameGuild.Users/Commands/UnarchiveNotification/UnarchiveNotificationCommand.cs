using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to unarchive a notification
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="NotificationId">Notification identifier</param>
public record UnarchiveNotificationCommand(Guid UserId, Guid NotificationId) : ICommand;

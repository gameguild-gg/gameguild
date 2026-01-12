using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to unarchive multiple notifications
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="NotificationIds">List of notification IDs to unarchive</param>
public record BulkUnarchiveNotificationsCommand(Guid UserId, List<Guid> NotificationIds) : ICommand;

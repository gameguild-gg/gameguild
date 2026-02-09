using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to update user notification preferences
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Notification preferences to update</param>
public sealed record UpdateUserNotificationPreferencesCommand(Guid UserId, UpdateUserNotificationPreferencesRequest Request) : ICommand;

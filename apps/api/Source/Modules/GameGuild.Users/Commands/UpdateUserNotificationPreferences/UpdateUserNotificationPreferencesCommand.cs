using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to update user notification preferences
/// </summary>
/// <param name="UserId">The user ID</param>
/// <param name="Request">Notification preferences to update</param>
public record UpdateUserNotificationPreferencesCommand(Guid UserId, UpdateUserNotificationPreferencesRequest Request) : ICommand;

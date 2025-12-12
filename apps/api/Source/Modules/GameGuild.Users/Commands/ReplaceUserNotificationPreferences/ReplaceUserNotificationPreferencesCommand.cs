using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

public record ReplaceUserNotificationPreferencesCommand(Guid UserId, ReplaceUserNotificationPreferencesRequest Request) : ICommand;

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ReplaceUserNotificationPreferencesCommand(Guid UserId, ReplaceUserNotificationPreferencesRequest Request) : ICommand;

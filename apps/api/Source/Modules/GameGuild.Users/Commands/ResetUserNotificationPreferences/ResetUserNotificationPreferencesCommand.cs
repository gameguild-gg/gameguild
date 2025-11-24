using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

public record ResetUserNotificationPreferencesCommand(Guid UserId) : ICommand;

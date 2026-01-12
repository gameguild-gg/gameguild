using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ResetUserNotificationPreferencesCommand(Guid UserId) : ICommand;

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ResetUserNotificationPreferencesCommand(Guid UserId) : ICommand;

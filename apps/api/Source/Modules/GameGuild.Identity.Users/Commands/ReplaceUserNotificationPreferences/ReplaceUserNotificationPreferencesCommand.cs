using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ReplaceUserNotificationPreferencesCommand(Guid UserId, ReplaceUserNotificationPreferencesRequest Request) : ICommand;

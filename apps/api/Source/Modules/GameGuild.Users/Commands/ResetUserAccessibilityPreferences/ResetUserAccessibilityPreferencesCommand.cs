using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

public record ResetUserAccessibilityPreferencesCommand(Guid UserId) : ICommand;

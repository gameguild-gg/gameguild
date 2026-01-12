using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ResetUserAccessibilityPreferencesCommand(Guid UserId) : ICommand;

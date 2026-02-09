using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ResetUserAccessibilityPreferencesCommand(Guid UserId) : ICommand;

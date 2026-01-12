using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ReplaceUserAccessibilityPreferencesCommand(Guid UserId, ReplaceUserAccessibilityPreferencesRequest Request) : ICommand;

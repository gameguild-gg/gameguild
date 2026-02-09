using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ReplaceUserAccessibilityPreferencesCommand(Guid UserId, ReplaceUserAccessibilityPreferencesRequest Request) : ICommand;

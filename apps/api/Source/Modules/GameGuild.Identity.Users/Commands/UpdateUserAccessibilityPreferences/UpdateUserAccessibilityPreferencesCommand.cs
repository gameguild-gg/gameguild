using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record UpdateUserAccessibilityPreferencesCommand(Guid UserId, UpdateUserAccessibilityPreferencesRequest Request) : ICommand;

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record UpdateUserAccessibilityPreferencesCommand(Guid UserId, UpdateUserAccessibilityPreferencesRequest Request) : ICommand;

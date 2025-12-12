using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

public record UpdateUserAccessibilityPreferencesCommand(Guid UserId, UpdateUserAccessibilityPreferencesRequest Request) : ICommand;

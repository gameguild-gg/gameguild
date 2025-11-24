using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

public record ReplaceUserAccessibilityPreferencesCommand(Guid UserId, ReplaceUserAccessibilityPreferencesRequest Request) : ICommand;

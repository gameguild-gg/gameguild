using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

public record ReplaceUserProfileCommand(Guid UserId, ReplaceUserProfileRequest Request) : ICommand;

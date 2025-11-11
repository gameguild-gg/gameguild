using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

public record UpdateUserProfileCommand(Guid UserId, UpdateUserProfileRequest Request) : ICommand;

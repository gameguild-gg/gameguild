using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record UpdateUserProfileCommand(Guid UserId, UpdateUserProfileRequest Request) : ICommand;

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public record ReplaceUserProfileCommand(Guid UserId, ReplaceUserProfileRequest Request) : ICommand;

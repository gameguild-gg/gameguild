using GameGuild.CQRS;

namespace GameGuild.Social.Profiles;

public record UpdateUserProfileCommand(Guid UserId, UpdateUserProfileRequest Request) : ICommand;

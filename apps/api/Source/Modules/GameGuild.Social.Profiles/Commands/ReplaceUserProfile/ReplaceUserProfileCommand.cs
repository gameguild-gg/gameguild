using GameGuild.CQRS;

namespace GameGuild.Social.Profiles;

public record ReplaceUserProfileCommand(Guid UserId, ReplaceUserProfileRequest Request) : ICommand;

using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record ReplaceUserProfileCommand(Guid UserId, ReplaceUserProfileRequest Request) : ICommand<UserProfileDto>;

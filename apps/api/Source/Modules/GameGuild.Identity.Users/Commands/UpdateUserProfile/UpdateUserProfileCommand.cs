using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed record UpdateUserProfileCommand(Guid UserId, UpdateUserProfileRequest Request) : ICommand<UserProfileDto>;

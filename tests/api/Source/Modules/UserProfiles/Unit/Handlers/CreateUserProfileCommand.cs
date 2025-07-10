namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Handlers;

public class CreateUserProfileCommand // : IRequest<UserProfile>
{
    public Guid UserId { get; set; }

    public string Bio { get; set; }

    public string AvatarUrl { get; set; }
}
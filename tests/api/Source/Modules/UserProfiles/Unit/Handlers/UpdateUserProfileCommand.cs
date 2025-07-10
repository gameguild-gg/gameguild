namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Handlers;

public class UpdateUserProfileCommand // : IRequest<UserProfile>
{
    public Guid Id { get; set; }

    public string Bio { get; set; }

    public string AvatarUrl { get; set; }

    public string Location { get; set; }
}
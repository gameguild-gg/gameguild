namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Controllers;

public class UserProfile {
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Bio { get; set; }

    public string AvatarUrl { get; set; }
}
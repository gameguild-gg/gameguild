namespace GameGuild.Modules.UserProfiles;

public class UpdateUserProfileRequest
{
    [StringLength(100, MinimumLength = 2)]
    public string? DisplayName { get; set; }
}

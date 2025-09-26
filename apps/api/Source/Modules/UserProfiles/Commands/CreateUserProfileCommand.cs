using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Command to create a new user profile with validation and business logic </summary>
public class CreateUserProfileCommand : ICommand<Result<UserProfile>>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }
}

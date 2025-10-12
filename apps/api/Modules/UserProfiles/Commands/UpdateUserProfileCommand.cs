using GameGuild.CQRS;

namespace GameGuild.Modules.UserProfiles;

/// <summary> Command to update user profile with validation and business logic </summary>
public class UpdateUserProfileCommand : ICommand<Result<UserProfile>>
{
    [Required]
    public Guid UserProfileId { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? DisplayName { get; set; }

    /// Expected version for optimistic concurrency control
    /// </summary>
    public int? ExpectedVersion { get; set; }
}

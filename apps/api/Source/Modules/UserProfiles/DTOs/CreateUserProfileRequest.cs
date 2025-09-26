namespace GameGuild.Modules.UserProfiles;

public class CreateUserProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The user ID this profile belongs to (required for 1:1 relationship)
    /// </summary>
    [Required]
    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }
}

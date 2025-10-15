using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Command to create a new user profile with validation and business logic
/// </summary>
public class CreateUserProfileCommand : ICommand<Common.Result<UserProfile>> {
  [Required]
  [StringLength(100, MinimumLength = 1)]
  public string GivenName { get; set; } = string.Empty;

  [Required]
  [StringLength(100, MinimumLength = 1)]
  public string FamilyName { get; set; } = string.Empty;

  [Required]
  [StringLength(100, MinimumLength = 2)]
  public string DisplayName { get; set; } = string.Empty;

  [StringLength(200)] public string? Title { get; set; }

  [StringLength(1000)] public string? Description { get; set; }

  public Guid UserId { get; set; }

  public Guid? TenantId { get; set; }
}

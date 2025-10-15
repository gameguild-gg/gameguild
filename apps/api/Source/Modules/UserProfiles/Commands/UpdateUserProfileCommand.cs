using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Command to update user profile with validation and business logic
/// </summary>
public class UpdateUserProfileCommand : ICommand<Common.Result<UserProfile>> {
  [Required] public Guid UserProfileId { get; set; }

  [StringLength(100, MinimumLength = 1)] public string? GivenName { get; set; }

  [StringLength(100, MinimumLength = 1)] public string? FamilyName { get; set; }

  [StringLength(100, MinimumLength = 2)] public string? DisplayName { get; set; }

  [StringLength(200)] public string? Title { get; set; }

  [StringLength(1000)] public string? Description { get; set; }

  /// Expected version for optimistic concurrency control
  /// </summary>
  public int? ExpectedVersion { get; set; }
}

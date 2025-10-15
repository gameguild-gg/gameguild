using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Query to get user profile by ID
/// </summary>
public class GetUserProfileByIdQuery : IQuery<Common.Result<UserProfile?>> {
  [Required] public Guid UserProfileId { get; set; }

  public bool IncludeDeleted { get; set; } = false;
}

using GameGuild.CQRS;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Query to get user profile by user ID
/// </summary>
public class GetUserProfileByUserIdQuery : IQuery<Result<UserProfile?>> {
  [Required] public Guid UserId { get; set; }

  public bool IncludeDeleted { get; set; } = false;
}

using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Modules.UserProfiles.Entities;


namespace GameGuild.Modules.UserProfiles.Queries;

/// <summary>
/// Query to get user profile by user ID
/// </summary>
public class GetUserProfileByUserIdQuery : IQuery<Common.Result<UserProfile?>> {
  [Required] public Guid UserId { get; set; }

  public bool IncludeDeleted { get; set; } = false;
}

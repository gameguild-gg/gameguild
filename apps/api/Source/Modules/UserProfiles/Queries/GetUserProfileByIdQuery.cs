using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Modules.UserProfiles.Entities;


namespace GameGuild.Modules.UserProfiles.Queries;

/// <summary>
/// Query to get user profile by ID
/// </summary>
public class GetUserProfileByIdQuery : IQuery<Common.Result<UserProfile?>>
{
  [Required]
  public Guid UserProfileId { get; set; }

  public bool IncludeDeleted { get; set; } = false;
}

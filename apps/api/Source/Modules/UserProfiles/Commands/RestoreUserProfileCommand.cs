using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Command to restore a soft-deleted user profile
/// </summary>
public class RestoreUserProfileCommand : ICommand<Common.Result<bool>> {
  [Required] public Guid UserProfileId { get; set; }
}


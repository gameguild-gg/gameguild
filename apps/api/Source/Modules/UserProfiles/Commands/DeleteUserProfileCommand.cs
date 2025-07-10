using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles.Commands;

/// <summary>
/// Command to delete a user profile
/// </summary>
public class DeleteUserProfileCommand : ICommand<Common.Result<bool>> {
  [Required] public Guid UserProfileId { get; set; }

  public bool SoftDelete { get; set; } = true;
}

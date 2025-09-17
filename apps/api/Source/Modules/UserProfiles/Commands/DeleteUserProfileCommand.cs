using GameGuild.CQRS;


namespace GameGuild.Modules.UserProfiles;

/// <summary> Command to delete a user profile </summary>
public class DeleteUserProfileCommand : ICommand<Result<bool>> {
  [Required] public Guid UserProfileId { get; set; }

  public bool SoftDelete { get; set; } = true;
}

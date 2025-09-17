using GameGuild.CQRS;


namespace GameGuild.Modules.UserProfiles;

/// <summary> Command to restore a soft-deleted user profile </summary>
public class RestoreUserProfileCommand : ICommand<Result<bool>> {
  [Required] public Guid UserProfileId { get; set; }
}

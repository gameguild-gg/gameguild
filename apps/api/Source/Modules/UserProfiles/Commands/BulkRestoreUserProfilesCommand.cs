using GameGuild.Common;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Command to bulk restore multiple soft-deleted user profiles
/// </summary>
public sealed class BulkRestoreUserProfilesCommand : ICommand<Common.Result<int>> {
  /// <summary>
  /// User profile IDs to restore
  /// </summary>
  public required IEnumerable<Guid> UserProfileIds { get; set; }

  /// <summary>
  /// Reason for restoration
  /// </summary>
  public string? Reason { get; set; }
}

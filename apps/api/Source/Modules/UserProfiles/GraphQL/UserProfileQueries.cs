using GameGuild.Modules.UserProfiles.Services;
using GameGuild.Modules.Users.GraphQL;


namespace GameGuild.Modules.UserProfiles.GraphQL;

/// <summary>
/// GraphQL queries for UserProfile module
/// </summary>
[ExtendObjectType<Query>]
public class UserProfileQueries {
  /// <summary>
  /// Get all user profiles (non-deleted only)
  /// </summary>
  public async Task<IEnumerable<Models.UserProfile>> GetUserProfiles([Service] IUserProfileService userProfileService) { return await userProfileService.GetAllUserProfilesAsync(); }

  /// <summary>
  /// Get a user profile by ID
  /// </summary>
  public async Task<Models.UserProfile?> GetUserProfileById([Service] IUserProfileService userProfileService, Guid id) { return await userProfileService.GetUserProfileByIdAsync(id); }

  /// <summary>
  /// Get a user profile by user ID
  /// </summary>
  public async Task<Models.UserProfile?> GetUserProfileByUserId(
    [Service] IUserProfileService userProfileService,
    Guid userId
  ) {
    return await userProfileService.GetUserProfileByUserIdAsync(userId);
  }

  /// <summary>
  /// Get soft-deleted user profiles
  /// </summary>
  public async Task<IEnumerable<Models.UserProfile>> GetDeletedUserProfiles(
    [Service] IUserProfileService userProfileService
  ) {
    return await userProfileService.GetDeletedUserProfilesAsync();
  }
}

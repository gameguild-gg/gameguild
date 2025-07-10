using GameGuild.Modules.UserProfiles.Entities;


namespace GameGuild.Modules.UserProfiles.Services;

public interface IUserProfileService {
  Task<IEnumerable<UserProfile>> GetAllUserProfilesAsync();

  Task<UserProfile?> GetUserProfileByIdAsync(Guid id);

  Task<UserProfile?> GetUserProfileByUserIdAsync(Guid userId);

  Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile);

  Task<UserProfile?> UpdateUserProfileAsync(Guid id, UserProfile userProfile);

  Task<bool> DeleteUserProfileAsync(Guid id);

  Task<bool> SoftDeleteUserProfileAsync(Guid id);

  Task<bool> RestoreUserProfileAsync(Guid id);

  Task<IEnumerable<UserProfile>> GetDeletedUserProfilesAsync();
}

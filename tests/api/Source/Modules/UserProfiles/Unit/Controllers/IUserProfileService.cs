namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Controllers;

public interface IUserProfileService {
    Task<UserProfile> GetUserProfileByIdAsync(Guid id);

    Task<UserProfile> GetUserProfileByUserIdAsync(Guid userId);

    Task<UserProfile> CreateUserProfileAsync(UserProfile profile);

    Task<UserProfile> UpdateUserProfileAsync(UserProfile profile);

    Task<bool> DeleteUserProfileAsync(Guid id);
}
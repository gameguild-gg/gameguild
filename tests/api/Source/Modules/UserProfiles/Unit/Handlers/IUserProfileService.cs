namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Handlers;

public interface IUserProfileService {
    Task<UserProfile> CreateUserProfileAsync(UserProfile profile);

    Task<UserProfile> UpdateUserProfileAsync(UserProfile profile);

    Task<bool> DeleteUserProfileAsync(Guid id);
}
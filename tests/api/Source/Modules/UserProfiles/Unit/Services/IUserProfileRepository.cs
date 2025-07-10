namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Services;

public interface IUserProfileRepository {
    Task<UserProfile> GetByIdAsync(Guid id);

    Task<UserProfile> GetByUserIdAsync(Guid userId);

    Task<UserProfile> AddAsync(UserProfile profile);

    Task<UserProfile> UpdateAsync(UserProfile profile);

    Task<bool> DeleteAsync(Guid id);
}
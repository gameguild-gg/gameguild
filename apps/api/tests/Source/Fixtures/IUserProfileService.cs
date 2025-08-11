namespace GameGuild.Tests.Fixtures;

public interface IUserProfileService {
    Task<TestUserProfileEntity> GetByIdAsync(string id);

    Task<TestUserProfileEntity> GetByUserIdAsync(string userId);

    Task<TestUserProfileEntity> CreateAsync(TestUserProfileEntity profile);

    Task<TestUserProfileEntity> UpdateAsync(string id, TestUserProfileEntity profile);

    Task<bool> DeleteAsync(string id);
}

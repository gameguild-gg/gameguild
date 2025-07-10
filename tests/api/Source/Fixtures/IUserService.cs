namespace GameGuild.API.Tests.Fixtures;

public interface IUserService {
    Task<TestUserEntity> GetByIdAsync(string id);

    Task<TestUserEntity> CreateAsync(TestUserEntity user);

    Task<TestUserEntity?> UpdateAsync(string id, TestUserEntity user);

    Task<bool> DeleteAsync(string id);
}
namespace GameGuild.API.Tests.Modules.UserProfiles.Unit.Services;

public interface IUserRepository {
    Task<User> GetByIdAsync(Guid id);
}
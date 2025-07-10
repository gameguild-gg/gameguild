namespace GameGuild.Modules.Users;

public interface IUserService {
  Task<IEnumerable<User>> GetAllUsersAsync();

  Task<User?> GetUserByIdAsync(Guid id);

  Task<User?> GetByEmailAsync(string email);

  Task<User> CreateUserAsync(User user);

  Task<User?> UpdateUserAsync(Guid id, User user);

  Task<bool> DeleteUserAsync(Guid id);

  Task<bool> SoftDeleteUserAsync(Guid id);

  Task<bool> RestoreUserAsync(Guid id);

  Task<IEnumerable<User>> GetDeletedUsersAsync();
}
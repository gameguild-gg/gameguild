using GameGuild.Database;


namespace GameGuild.Modules.Users;

public class UserService(ApplicationDbContext context) : IUserService {
  public async Task<IEnumerable<User>> GetAllUsersAsync() { return await context.Users.Where(u => u.DeletedAt == null).ToListAsync(); }

  public async Task<User?> GetUserByIdAsync(Guid id) { return await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null); }

  public async Task<User?> GetByEmailAsync(string email) { return await context.Users.FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null); }

  public async Task<User> CreateUserAsync(User user) {
    // Check if email already exists
    var existingUser = await GetByEmailAsync(user.Email);

    if (existingUser != null) throw new InvalidOperationException($"A user with email '{user.Email}' already exists.");

    context.Users.Add(user);
    await context.SaveChangesAsync();

    return user;
  }

  public async Task<User?> UpdateUserAsync(Guid id, User user) {
    var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

    if (existingUser == null) return null;

    existingUser.Name = user.Name;
    existingUser.Email = user.Email;
    existingUser.IsActive = user.IsActive;

    await context.SaveChangesAsync();

    return existingUser;
  }

  public async Task<bool> DeleteUserAsync(Guid id) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);

    if (user == null) return false;

    context.Users.Remove(user);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> SoftDeleteUserAsync(Guid id) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

    if (user == null) return false;

    user.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreUserAsync(Guid id) {
    // Need to include deleted entities to find soft-deleted user
    var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt != null);

    if (user == null) return false;

    user.Restore();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<User>> GetDeletedUsersAsync() { return await context.Users.IgnoreQueryFilters().Where(u => u.DeletedAt != null).ToListAsync(); }
}

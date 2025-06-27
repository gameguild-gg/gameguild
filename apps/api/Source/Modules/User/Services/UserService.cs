using GameGuild.Data;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.User.Services;

public interface IUserService {
  Task<IEnumerable<Models.User>> GetAllUsersAsync();

  Task<Models.User?> GetUserByIdAsync(Guid id);

  Task<Models.User> CreateUserAsync(Models.User user);

  Task<Models.User?> UpdateUserAsync(Guid id, Models.User user);

  Task<bool> DeleteUserAsync(Guid id);

  Task<bool> SoftDeleteUserAsync(Guid id);

  Task<bool> RestoreUserAsync(Guid id);

  Task<IEnumerable<Models.User>> GetDeletedUsersAsync();
}

public class UserService(ApplicationDbContext context) : IUserService {
  public async Task<IEnumerable<Models.User>> GetAllUsersAsync() { return await context.Users.ToListAsync(); }

  public async Task<Models.User?> GetUserByIdAsync(Guid id) { return await context.Users.FindAsync(id); }

  public async Task<Models.User> CreateUserAsync(Models.User user) {
    context.Users.Add(user);
    await context.SaveChangesAsync();

    return user;
  }

  public async Task<Models.User?> UpdateUserAsync(Guid id, Models.User user) {
    var existingUser = await context.Users.FindAsync(id);

    if (existingUser == null) return null;

    existingUser.Name = user.Name;
    existingUser.Email = user.Email;
    existingUser.IsActive = user.IsActive;

    await context.SaveChangesAsync();

    return existingUser;
  }

  public async Task<bool> DeleteUserAsync(Guid id) {
    var user = await context.Users.FindAsync(id);

    if (user == null) return false;

    context.Users.Remove(user);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> SoftDeleteUserAsync(Guid id) {
    var user = await context.Users.FindAsync(id);

    if (user == null) return false;

    user.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreUserAsync(Guid id) {
    // Need to include deleted entities to find soft-deleted user
    var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);

    if (user == null || !user.IsDeleted) return false;

    user.Restore();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<Models.User>> GetDeletedUsersAsync() { return await context.Users.IgnoreQueryFilters().Where(u => u.DeletedAt != null).ToListAsync(); }
}

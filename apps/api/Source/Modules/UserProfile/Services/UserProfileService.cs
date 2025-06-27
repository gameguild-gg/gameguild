using GameGuild.Data;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfile.Services;

public interface IUserProfileService {
  Task<IEnumerable<Models.UserProfile>> GetAllUserProfilesAsync();

  Task<Models.UserProfile?> GetUserProfileByIdAsync(Guid id);

  Task<Models.UserProfile?> GetUserProfileByUserIdAsync(Guid userId);

  Task<Models.UserProfile> CreateUserProfileAsync(Models.UserProfile userProfile);

  Task<Models.UserProfile?> UpdateUserProfileAsync(Guid id, Models.UserProfile userProfile);

  Task<bool> DeleteUserProfileAsync(Guid id);

  Task<bool> SoftDeleteUserProfileAsync(Guid id);

  Task<bool> RestoreUserProfileAsync(Guid id);

  Task<IEnumerable<Models.UserProfile>> GetDeletedUserProfilesAsync();
}

public class UserProfileService(ApplicationDbContext context) : IUserProfileService {
  public async Task<IEnumerable<Models.UserProfile>> GetAllUserProfilesAsync() {
    return await context.Resources.OfType<Models.UserProfile>()
                         .Where(up => up.DeletedAt == null)
                         .Include(up => up.Metadata)
                         .ToListAsync();
  }

  public async Task<Models.UserProfile?> GetUserProfileByIdAsync(Guid id) {
    return await context.Resources.OfType<Models.UserProfile>()
                         .Include(up => up.Metadata)
                         .FirstOrDefaultAsync(up => up.Id == id && up.DeletedAt == null);
  }

  public async Task<Models.UserProfile?> GetUserProfileByUserIdAsync(Guid userId) {
    return await context.Resources.OfType<Models.UserProfile>()
                         .Include(up => up.Metadata)
                         .FirstOrDefaultAsync(up => up.Id == userId && up.DeletedAt == null);
  }

  public async Task<Models.UserProfile> CreateUserProfileAsync(Models.UserProfile userProfile) {
    context.Resources.Add(userProfile);
    await context.SaveChangesAsync();

    return userProfile;
  }

  public async Task<Models.UserProfile?> UpdateUserProfileAsync(Guid id, Models.UserProfile userProfile) {
    var existingProfile =
      await context.Resources.OfType<Models.UserProfile>().FirstOrDefaultAsync(up => up.Id == id);

    if (existingProfile == null || existingProfile.DeletedAt != null) return null;

    existingProfile.GivenName = userProfile.GivenName;
    existingProfile.FamilyName = userProfile.FamilyName;
    existingProfile.DisplayName = userProfile.DisplayName;
    existingProfile.Title = userProfile.Title;
    existingProfile.Description = userProfile.Description;

    await context.SaveChangesAsync();

    return existingProfile;
  }

  public async Task<bool> DeleteUserProfileAsync(Guid id) {
    var userProfile =
      await context.Resources.OfType<Models.UserProfile>().FirstOrDefaultAsync(up => up.Id == id);

    if (userProfile == null) return false;

    context.Resources.Remove(userProfile);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> SoftDeleteUserProfileAsync(Guid id) {
    var userProfile =
      await context.Resources.OfType<Models.UserProfile>().FirstOrDefaultAsync(up => up.Id == id);

    if (userProfile == null || userProfile.DeletedAt != null) return false;

    userProfile.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreUserProfileAsync(Guid id) {
    var userProfile = await context.Resources.OfType<Models.UserProfile>()
                                    .IgnoreQueryFilters()
                                    .FirstOrDefaultAsync(up => up.Id == id);

    if (userProfile == null || userProfile.DeletedAt == null) return false;

    userProfile.Restore();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<Models.UserProfile>> GetDeletedUserProfilesAsync() {
    return await context.Resources.OfType<Models.UserProfile>()
                         .IgnoreQueryFilters()
                         .Where(up => up.DeletedAt != null)
                         .Include(up => up.Metadata)
                         .ToListAsync();
  }
}

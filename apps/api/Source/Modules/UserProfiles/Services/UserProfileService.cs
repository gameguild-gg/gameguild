using GameGuild.Database;
using GameGuild.Modules.UserProfiles.Entities;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.UserProfiles.Services;

public class UserProfileService(ApplicationDbContext context) : IUserProfileService {
  public async Task<IEnumerable<UserProfile>> GetAllUserProfilesAsync() {
    return await context.Resources.OfType<UserProfile>()
                        .Where(up => up.DeletedAt == null)
                        .Include(up => up.Metadata)
                        .ToListAsync();
  }

  public async Task<UserProfile?> GetUserProfileByIdAsync(Guid id) {
    return await context.Resources.OfType<UserProfile>()
                        .Include(up => up.Metadata)
                        .FirstOrDefaultAsync(up => up.Id == id && up.DeletedAt == null);
  }

  public async Task<UserProfile?> GetUserProfileByUserIdAsync(Guid userId) {
    return await context.Resources.OfType<UserProfile>()
                        .Include(up => up.Metadata)
                        .FirstOrDefaultAsync(up => up.Id == userId && up.DeletedAt == null);
  }

  public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile) {
    context.Resources.Add(userProfile);
    await context.SaveChangesAsync();

    return userProfile;
  }

  public async Task<UserProfile?> UpdateUserProfileAsync(Guid id, UserProfile userProfile) {
    var existingProfile =
      await context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(up => up.Id == id);

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
      await context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(up => up.Id == id);

    if (userProfile == null) return false;

    context.Resources.Remove(userProfile);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> SoftDeleteUserProfileAsync(Guid id) {
    var userProfile =
      await context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(up => up.Id == id);

    if (userProfile == null || userProfile.DeletedAt != null) return false;

    userProfile.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreUserProfileAsync(Guid id) {
    var userProfile = await context.Resources.OfType<UserProfile>()
                                   .IgnoreQueryFilters()
                                   .FirstOrDefaultAsync(up => up.Id == id);

    if (userProfile == null || userProfile.DeletedAt == null) return false;

    userProfile.Restore();
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<UserProfile>> GetDeletedUserProfilesAsync() {
    return await context.Resources.OfType<UserProfile>()
                        .IgnoreQueryFilters()
                        .Where(up => up.DeletedAt != null)
                        .Include(up => up.Metadata)
                        .ToListAsync();
  }
}

using Microsoft.EntityFrameworkCore;


namespace GameGuild.Tests.Fixtures;

public class TestUserProfileService(TestDbContext dbContext) : IUserProfileService {
    public async Task<TestUserProfileEntity> GetByIdAsync(string id) { 
        return await dbContext.UserProfiles.FindAsync(id) ?? throw new InvalidOperationException($"UserProfile with ID {id} not found"); 
    }

    public async Task<TestUserProfileEntity> GetByUserIdAsync(string userId) {
        // Use LINQ to find by UserId
        return await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId) 
            ?? throw new InvalidOperationException($"UserProfile with UserId {userId} not found");
    }

    public async Task<TestUserProfileEntity> CreateAsync(TestUserProfileEntity profile) {
        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        return profile;
    }

    public async Task<TestUserProfileEntity> UpdateAsync(string id, TestUserProfileEntity profile) {
        var existingProfile = await dbContext.UserProfiles.FindAsync(id);

        if (existingProfile == null) throw new InvalidOperationException($"UserProfile with ID {id} not found");

        existingProfile.Bio = profile.Bio;
        existingProfile.AvatarUrl = profile.AvatarUrl;
        existingProfile.Location = profile.Location;
        existingProfile.WebsiteUrl = profile.WebsiteUrl;
        existingProfile.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return existingProfile;
    }

    public async Task<bool> DeleteAsync(string id) {
        var profile = await dbContext.UserProfiles.FindAsync(id);

        if (profile == null) return false;

        dbContext.UserProfiles.Remove(profile);
        await dbContext.SaveChangesAsync();

        return true;
    }
}

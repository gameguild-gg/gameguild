namespace GameGuild.Tests.Fixtures;

public class TestUserService(TestDbContext dbContext) : IUserService {
    public async Task<TestUserEntity> GetByIdAsync(string id) { 
        return await dbContext.Users.FindAsync(id) ?? throw new InvalidOperationException($"User with ID {id} not found"); 
    }

    public async Task<TestUserEntity> CreateAsync(TestUserEntity user) {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<TestUserEntity?> UpdateAsync(string id, TestUserEntity user) {
        var existingUser = await dbContext.Users.FindAsync(id);

        if (existingUser == null) return null;

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return existingUser;
    }

    public async Task<bool> DeleteAsync(string id) {
        var user = await dbContext.Users.FindAsync(id);

        if (user == null) return false;

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return true;
    }
}

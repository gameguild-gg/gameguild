using Microsoft.EntityFrameworkCore;


namespace GameGuild.Tests.Fixtures;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options) {
    public DbSet<TestUserEntity> Users { get; set; }

    public DbSet<TestUserProfileEntity> UserProfiles { get; set; }
}
